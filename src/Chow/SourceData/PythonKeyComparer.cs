using System;
using System.Runtime.CompilerServices;
using Chow.Code;
using Chow.Interpreter.Exceptions;

namespace Chow.SourceData
{
    /// <summary>
    /// Compares and hashes dict keys the way Python does: equal numbers are the same key across
    /// bool, int, and float, so <c>1</c>, <c>1.0</c>, and <c>True</c> collide.
    /// <see cref="SourceValue.Equals(SourceValue)"/> stays type-strict because the constant pool and
    /// the public API depend on it.
    /// </summary>
    static class PythonKeyComparer
    {
        // 2^63: integral doubles at or past it don't fit in a long.
        const double LongExclusiveUpperBound = 9223372036854775808.0;

        /// <summary>
        /// Whether two hashable keys are the same key. Equal keys always have equal
        /// <see cref="GetHash"/> values.
        /// </summary>
        public static bool AreEqual(SourceValue x, SourceValue y)
        {
            if (IsNumeric(x.DataType) && IsNumeric(y.DataType))
            {
                return NumbersEqual(x, y);
            }

            if (x.DataType != y.DataType)
            {
                return false;
            }

            switch (x.DataType)
            {
                case DataType.None:
                    return true;
                case DataType.Str:
                    return string.Equals((string)x.ToObject(), (string)y.ToObject(), StringComparison.Ordinal);
                case DataType.FrozenSet:
                    // Same elements, regardless of order.
                    return ((SourceSet)x.ToObject()).EqualsTo((SourceSet)y.ToObject());
                default:
                    return ReferenceEquals(x.ToObject(), y.ToObject());
            }
        }

        public static int GetHash(SourceValue key)
        {
            switch (key.DataType)
            {
                case DataType.None:
                    return 0;
                case DataType.Bool:
                case DataType.Long:
                    return key.ToLong().GetHashCode();
                case DataType.Double:
                {
                    var value = key.ToDouble();

                    // Integral doubles hash like the equal int, so 1.0 and -0.0 land with 1 and 0.
                    return IsLongValued(value)
                        ? ((long)value).GetHashCode()
                        : BitConverter.DoubleToInt64Bits(value).GetHashCode();
                }
                case DataType.Str:
                    return key.ToObject().GetHashCode();
                case DataType.FrozenSet:
                    return ((SourceSet)key.ToObject()).FrozenSetHash.GetHashCode();
                default:
                    return RuntimeHelpers.GetHashCode(key.ToObject());
            }
        }

        /// <summary>
        /// Throws a <c>TypeError</c> if <paramref name="key"/> cannot be used as a dict key or set element.
        /// </summary>
        public static void ValidateHashable(SourceValue key)
        {
            switch (key.DataType)
            {
                case DataType.None:
                case DataType.Bool:
                case DataType.Long:
                case DataType.Double:
                case DataType.Str:
                case DataType.DictValues:
                case DataType.Iterator:
                case DataType.FrozenSet:
                    return;
                default:
                    throw new DataTypeException($"unhashable type: '{DataTypeNames.GetTypeName(key)}'");
            }
        }

        // Exact, as in Python: 2**53 + 1 is not 2.0**53, even though converting it to float would say so.
        static bool NumbersEqual(SourceValue x, SourceValue y)
        {
            var isXDouble = x.DataType == DataType.Double;
            var isYDouble = y.DataType == DataType.Double;

            if (!isXDouble && !isYDouble)
            {
                return x.ToLong() == y.ToLong();
            }

            if (isXDouble && isYDouble)
            {
                var l = x.ToDouble();
                var r = y.ToDouble();

                // A NaN has no object identity in a SourceValue, so it matches a NaN with the same bits.
                return l == r || BitConverter.DoubleToInt64Bits(l) == BitConverter.DoubleToInt64Bits(r);
            }

            var dbl = isXDouble ? x.ToDouble() : y.ToDouble();
            var lng = isXDouble ? y.ToLong() : x.ToLong();
            return IsLongValued(dbl) && (long)dbl == lng;
        }

        static bool IsLongValued(double value)
        {
            return Math.Floor(value) == value && value >= long.MinValue && value < LongExclusiveUpperBound;
        }

        static bool IsNumeric(DataType dataType)
        {
            return dataType == DataType.Bool || dataType == DataType.Long || dataType == DataType.Double;
        }
    }
}
