using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Chow.Code;
using Chow.Interpreter.Exceptions;

namespace Chow.SourceData
{
    /// <summary>
    /// Compares dict keys the way Python does: equal numbers are the same key across bool, int,
    /// and float, so <c>1</c>, <c>1.0</c>, and <c>True</c> collide. <see cref="SourceValue.Equals(SourceValue)"/>
    /// stays type-strict because the constant pool and the public API depend on it.
    /// </summary>
    sealed class PythonKeyComparer : IEqualityComparer<SourceValue>
    {
        // 2^63: integral doubles at or past it don't fit in a long.
        const double LongExclusiveUpperBound = 9223372036854775808.0;

        public static readonly PythonKeyComparer Instance = new PythonKeyComparer();

        PythonKeyComparer()
        {
        }

        public bool Equals(SourceValue x, SourceValue y)
        {
            if (IsNumeric(x.DataType) && IsNumeric(y.DataType))
            {
                if (x.DataType != DataType.Double && y.DataType != DataType.Double)
                {
                    return x.ToLong() == y.ToLong();
                }

                var l = x.ToDouble();
                var r = y.ToDouble();

                // A NaN has no object identity in a SourceValue, so it matches a NaN with the same bits.
                return l == r || BitConverter.DoubleToInt64Bits(l) == BitConverter.DoubleToInt64Bits(r);
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
                    return string.Equals(x.ToString(), y.ToString(), StringComparison.Ordinal);
                default:
                    return ReferenceEquals(x.ToObject(), y.ToObject());
            }
        }

        public int GetHashCode(SourceValue obj)
        {
            switch (obj.DataType)
            {
                case DataType.None:
                    return 0;
                case DataType.Bool:
                case DataType.Long:
                    return obj.ToLong().GetHashCode();
                case DataType.Double:
                {
                    var value = obj.ToDouble();

                    // Integral doubles hash like the equal int, so 1.0 and -0.0 land with 1 and 0.
                    if (Math.Floor(value) == value && value >= long.MinValue && value < LongExclusiveUpperBound)
                    {
                        return ((long)value).GetHashCode();
                    }

                    return BitConverter.DoubleToInt64Bits(value).GetHashCode();
                }
                case DataType.Str:
                    return StringComparer.Ordinal.GetHashCode(obj.ToString());
                default:
                    return RuntimeHelpers.GetHashCode(obj.ToObject());
            }
        }

        /// <summary>
        /// Throws a <c>TypeError</c> if <paramref name="key"/> cannot be used as a dict key.
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
                    return;
                default:
                    throw new DataTypeException($"unhashable type: '{DataTypeNames.GetTypeName(key.DataType)}'");
            }
        }

        static bool IsNumeric(DataType dataType)
        {
            return dataType == DataType.Bool || dataType == DataType.Long || dataType == DataType.Double;
        }
    }
}
