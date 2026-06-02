using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Chow.DataTypes;
using Chow.Exceptions;
namespace Chow
{
    /// <summary>
    /// Represents an immutable Chow value of varying Chow data types, with the main types being:
    /// <b>int, float, str, bool, None, list, dict, and range</b>.
    /// </summary>
    public readonly struct ChowValue
    {
        // TODO: Make helper function that is operator agnostic for converting operands to different types

        #region Fields

        static readonly Dictionary<Type, DataType> DataTypeMap = new Dictionary<Type, DataType>
        {
            { typeof(bool), DataType.Bool },
            { typeof(long), DataType.Int },
            { typeof(int), DataType.Int },
            { typeof(double), DataType.Float },
            { typeof(string), DataType.Str },
            { typeof(InternalDict), DataType.Dict },
            { typeof(InternalRange), DataType.Range },
            { typeof(InternalList), DataType.List }
        };

        /// <summary>Represents the ChowValue equivalent to null/nil/none values.</summary>
        public static readonly ChowValue None = new ChowValue(DataType.None);

        readonly bool _boolValue;
        readonly object _objectValue;

        // In Chow integers are 64 bits instead of 32 bits like C# (hence why we're calling the
        // long type its formal name)
        readonly long _int64Value;

        // Naming convention is for a similar reason to _int64Value
        readonly double _float64Value;

        #endregion

        #region Properties

        internal DataType DataType { get; }

        bool IsNullableType =>
            DataType == DataType.Object || DataType == DataType.List || DataType == DataType.Dict || DataType == DataType.Range ||
            DataType == DataType.Str;

        #endregion

        #region Constructors

        ChowValue(
            DataType dataType = DataType.None,
            bool boolValue = DEFAULT_BOOL_VALUE,
            object objectValue = DEFAULT_OBJECT_VALUE,
            long int64Value = DEFAULT_INT64_VALUE,
            double float64Value = DEFAULT_FLOAT64_VALUE)
        {
            DataType = dataType;
            _boolValue = boolValue;
            _objectValue = objectValue;
            _int64Value = int64Value;
            _float64Value = float64Value;

            if (IsNullableType && _objectValue == null)
            {
                throw new ArgumentNullException(nameof(objectValue));
            }
        }

        internal ChowValue(long value) : this(DataType.Int, int64Value: value) {}

        internal ChowValue(double value) : this(DataType.Float, float64Value: value) {}

        internal ChowValue(bool value) : this(DataType.Bool, value) {}

        internal ChowValue(string value) : this(DataType.Str, objectValue: value) {}

        internal ChowValue(InternalList list) : this(DataType.List, objectValue: list) {}

        internal ChowValue(InternalDict dict) : this(DataType.Dict, objectValue: dict) {}

        internal ChowValue(InternalRange range) : this(DataType.Range, objectValue: range) {}

        // Re-dispatches to a typed ctor when obj happens to be a recognized interpreter value, so callers
        // holding an `object` reference don't accidentally land in Tag.Object. Unknown types (interop
        // delegates, ClosureTemplate, IChowIterator, etc.) keep the Object tag as intended.
        internal ChowValue(object obj)
        {
            switch (obj)
            {
                case null:
                {
                    throw new ArgumentNullException(nameof(obj));
                }
                case string strValue:
                {
                    this = new ChowValue(strValue);
                    return;
                }
                case long longValue:
                {
                    this = new ChowValue(longValue);
                    return;
                }
                case int intValue:
                {
                    this = new ChowValue(intValue);
                    return;
                }
                case double doubleValue:
                {
                    this = new ChowValue(doubleValue);
                    return;
                }
                case bool boolValue:
                {
                    this = new ChowValue(boolValue);
                    return;
                }
                case InternalList listValue:
                {
                    this = new ChowValue(listValue);
                    return;
                }
                case InternalDict dictValue:
                {
                    this = new ChowValue(dictValue);
                    return;
                }
                case InternalRange rangeValue:
                {
                    this = new ChowValue(rangeValue);
                    return;
                }
                case ChowValue chowValue:
                {
                    // **IMPORTANT**: CHOW VALUES ARE NEVER DIRECTLY WRAPPED IN OTHER CHOW VALUE INSTANCES
                    this = chowValue;
                    return;
                }
                default:
                {
                    DataType = DataType.Object;
                    _boolValue = DEFAULT_BOOL_VALUE;
                    _objectValue = obj;
                    _int64Value = DEFAULT_INT64_VALUE;
                    _float64Value = DEFAULT_FLOAT64_VALUE;
                    return;
                }
            }
        }

        #endregion

        #region Type API

        /// <summary>Casts Chow value to specified host type, boxes, and returns it.</summary>
        /// <typeparam name="TDataType">The type the value will be casted to.</typeparam>
        /// <returns>The boxed and converted Chow value.</returns>
        /// <exception cref="InvalidOperationException">Throws an exception if the value stored 
        /// in this instance cannot be converted to the target type.</exception>
        public TDataType AsType<TDataType>()
        {
            var typeOf = typeof(TDataType);

            if (typeOf == typeof(object))
            {
                return (TDataType)ToObject();
            }

            if (!DataTypeMap.TryGetValue(typeOf, out var targetDataType))
            {
                if (_objectValue is TDataType typedObject)
                {
                    return typedObject;
                }

                throw new InvalidOperationException($"Cannot convert {DataType} to {typeOf}");
            }

            switch (targetDataType)
            {
                case DataType.Bool:
                {
                    return (TDataType)(object)ToBool();
                }
                case DataType.Int:
                {
                    // The map aliases both typeof(long) and typeof(int) to DataType.Int.
                    // For T == int we truncate; for T == long we return the full 64-bit value.
                    if (typeOf == typeof(int))
                    {
                        // TODO: Add error checking for overflow scenarios
                        return (TDataType)(object)(int)ToInt64();
                    }

                    return (TDataType)(object)ToInt64();
                }
                case DataType.Float:
                {
                    return (TDataType)(object)ToFloat64();
                }
                case DataType.Str:
                {
                    return (TDataType)(object)ToStr();
                }
                case DataType.List:
                case DataType.Dict:
                case DataType.Range:
                {
                    if (_objectValue is TDataType typedObject)
                    {
                        return typedObject;
                    }

                    break;
                }
            }

            throw new InvalidOperationException($"Cannot convert {DataType} to {typeof(TDataType)}");
        }

        /// <summary>Whether the Chow value of this instance is of the provided data type.</summary>
        /// <typeparam name="TDataType">The data type to compare to the Chow value's data type.</typeparam>
        /// <returns>True if the Chow value of this instance is of type <typeparamref name=
        /// "TDataType"/>; otherwise, false.</returns>
        public bool IsOfType<TDataType>()
        {
            var checkType = typeof(TDataType);

            // If it is not a type defined by the DataType enum
            if (!DataTypeMap.TryGetValue(checkType, out var chowDataType))
            {
                return DataType == DataType.Object && _objectValue is TDataType;
            }

            // The map includes values representing data types that are from the Chow namespace
            return DataType == chowDataType;
        }

        public bool IsTruthy()
        {
            return ToBool();
        }

        #endregion

        #region Arithmetic & Logical Operations

        // Instance methods to avoid passing two ChowValues as parameters. Each returns a new ChowValue
        // (the struct is readonly, so no risk of accidentally mutating this instance's internal state).
        // Promotion rules come from DataTypeConversionMap (the single source of truth). Carve-outs for
        // container/string ops (list+list, list*int, str+str, str*int, dict|dict) are dispatched when
        // the map reports ConversionCase.Nothing.

        internal ChowValue CreateSum(ChowValue rightOperand)
        {
            switch (LookupBinary(ExpressionOp.Add, rightOperand))
            {
                case ConversionCase.ToInt:
                {
                    return new ChowValue(PromoteToInt64() + rightOperand.PromoteToInt64());
                }
                case ConversionCase.ToFloat:
                {
                    return new ChowValue(PromoteToFloat64() + rightOperand.PromoteToFloat64());
                }
                case ConversionCase.Nothing:
                {
                    if (DataType == DataType.List && rightOperand.DataType == DataType.List)
                    {
                        return new ChowValue(InternalList.Concat(AsType<InternalList>(), rightOperand.AsType<InternalList>()));
                    }

                    if (DataType == DataType.Str && rightOperand.DataType == DataType.Str)
                    {
                        return new ChowValue(AsType<string>() + rightOperand.AsType<string>());
                    }

                    break;
                }
            }

            throw UnsupportedBinary(ExpressionOp.Add, rightOperand);
        }

        internal ChowValue CreateDifference(ChowValue rightOperand)
        {
            if (LookupBinary(ExpressionOp.Subtract, rightOperand) == ConversionCase.ToInt)
            {
                return new ChowValue(PromoteToInt64() - rightOperand.PromoteToInt64());
            }
            else if (LookupBinary(ExpressionOp.Subtract, rightOperand) == ConversionCase.ToFloat)
            {
                return new ChowValue(PromoteToFloat64() - rightOperand.PromoteToFloat64());
            }
            else if (LookupBinary(ExpressionOp.Subtract, rightOperand) == ConversionCase.Nothing)
            {
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }

            throw UnsupportedBinary(ExpressionOp.Subtract, rightOperand);
        }

        internal ChowValue CreateProduct(ChowValue rightOperand)
        {
            switch (LookupBinary(ExpressionOp.Multiply, rightOperand))
            {
                case ConversionCase.ToInt:
                {
                    return new ChowValue(PromoteToInt64() * rightOperand.PromoteToInt64());
                }
                case ConversionCase.ToFloat:
                {
                    return new ChowValue(PromoteToFloat64() * rightOperand.PromoteToFloat64());
                }
                case ConversionCase.Nothing:
                {
                    // Python treats bool as a subtype of int, so [1] * True and "ab" * True are valid.
                    if (DataType == DataType.List && IsIntegerTag(rightOperand.DataType))
                    {
                        return new ChowValue(InternalList.Repeat(AsType<InternalList>(), rightOperand.AsType<int>()));
                    }

                    if (IsIntegerTag(DataType) && rightOperand.DataType == DataType.List)
                    {
                        return new ChowValue(InternalList.Repeat(rightOperand.AsType<InternalList>(), AsType<int>()));
                    }

                    if (DataType == DataType.Str && IsIntegerTag(rightOperand.DataType))
                    {
                        return new ChowValue(RepeatString(AsType<string>(), rightOperand.AsType<int>()));
                    }

                    if (IsIntegerTag(DataType) && rightOperand.DataType == DataType.Str)
                    {
                        return new ChowValue(RepeatString(rightOperand.AsType<string>(), AsType<int>()));
                    }

                    break;
                }
            }

            throw UnsupportedBinary(ExpressionOp.Multiply, rightOperand);
        }

        internal ChowValue CreateQuotient(ChowValue rightOperand)
        {
            // Python semantics: `/` always produces a float, even for int / int.
            switch (LookupBinary(ExpressionOp.Divide, rightOperand))
            {
                case ConversionCase.ToFloat:
                {
                    var divisor = rightOperand.PromoteToFloat64();

                    return divisor == 0.0 
                        ? throw new ZeroDivisionException() 
                        : new ChowValue(PromoteToFloat64() / divisor);

                }
                case ConversionCase.Nothing:
                case ConversionCase.ToInt:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            throw UnsupportedBinary(ExpressionOp.Divide, rightOperand);
        }

        internal ChowValue CreateModulus(ChowValue rightOperand)
        {
            // Python semantics: result has the sign of the divisor.
            switch (LookupBinary(ExpressionOp.Modulus, rightOperand))
            {
                case ConversionCase.ToInt:
                {
                    var a = PromoteToInt64();
                    var b = rightOperand.PromoteToInt64();

                    if (b == 0L)
                    {
                        throw new ZeroDivisionException();
                    }

                    return new ChowValue((a % b + b) % b);
                }
                case ConversionCase.ToFloat:
                {
                    var l = PromoteToFloat64();
                    var r = rightOperand.PromoteToFloat64();

                    if (r == 0.0)
                    {
                        throw new ZeroDivisionException();
                    }

                    return new ChowValue((l % r + r) % r);
                }
            }

            throw UnsupportedBinary(ExpressionOp.Modulus, rightOperand);
        }

        internal ChowValue CreateFloorQuotient(ChowValue rightOperand)
        {
            // Python semantics: floors toward negative infinity. Integer path stays in longs (no detour
            // through double) so values past 2^53 remain exact.
            switch (LookupBinary(ExpressionOp.FloorDivide, rightOperand))
            {
                case ConversionCase.ToInt:
                {
                    var a = PromoteToInt64();
                    var b = rightOperand.PromoteToInt64();

                    if (b == 0L)
                    {
                        throw new ZeroDivisionException();
                    }

                    var q = a / b;

                    if (a % b != 0L && a < 0L != b < 0L)
                    {
                        q--;
                    }

                    return new ChowValue(q);
                }
                case ConversionCase.ToFloat:
                {
                    var divisor = rightOperand.PromoteToFloat64();

                    if (divisor == 0.0)
                    {
                        throw new ZeroDivisionException();
                    }

                    return new ChowValue(Math.Floor(PromoteToFloat64() / divisor));
                }
            }

            throw UnsupportedBinary(ExpressionOp.FloorDivide, rightOperand);
        }

        internal ChowValue CreatePower(ChowValue rightOperand)
        {
            // Python semantics: float if either operand is float, or if exponent is negative.
            // This is the one documented map override: the negative-exponent rule is value-dependent
            // (depends on the runtime exponent's sign), not type-dependent, so it cannot live in the
            // type-keyed map. Every other dispatch path defers to DataTypeConversionMap.
            var conv = LookupBinary(ExpressionOp.Exponentiate, rightOperand);

            if (conv == ConversionCase.ToInt && rightOperand.PromoteToInt64() < 0)
            {
                conv = ConversionCase.ToFloat;
            }

            switch (conv)
            {
                case ConversionCase.ToInt:
                {
                    // Exponent is non-negative here (negative-exp routed to float above). Exact integer
                    // exponentiation avoids the 2^53 precision ceiling of (long)Math.Pow. Overflow wraps
                    // silently — matches prior behavior; arbitrary-precision int is a separate concern.
                    return new ChowValue(IntPow(PromoteToInt64(), rightOperand.PromoteToInt64()));
                }
                case ConversionCase.ToFloat:
                {
                    return new ChowValue(Math.Pow(PromoteToFloat64(), rightOperand.PromoteToFloat64()));
                }
            }

            throw UnsupportedBinary(ExpressionOp.Exponentiate, rightOperand);
        }

        internal ChowValue CreateUnion(ChowValue rightOperand)
        {
            if (LookupBinary(ExpressionOp.BinaryOr, rightOperand) == ConversionCase.Nothing
                && DataType == DataType.Dict && rightOperand.DataType == DataType.Dict)
            {
                return new ChowValue(InternalDict.Merge(AsType<InternalDict>(), rightOperand.AsType<InternalDict>()));
            }

            throw UnsupportedBinary(ExpressionOp.BinaryOr, rightOperand);
        }

        internal ChowValue CreateNegation()
        {
            switch (LookupUnary(ExpressionOp.Negate))
            {
                case ConversionCase.ToInt:
                {
                    return new ChowValue(-PromoteToInt64());
                }
                case ConversionCase.ToFloat:
                {
                    return new ChowValue(-PromoteToFloat64());
                }
            }

            throw UnsupportedUnary(ExpressionOp.Negate);
        }

        internal ChowValue CreateLogicalNot()
        {
            // The map records this as Nothing for every type; consult it for consistency and so that
            // a future map change (e.g. restricting unary `not` to specific types) propagates here.
            LookupUnary(ExpressionOp.Not);
            return new ChowValue(!IsTruthy());
        }

        internal ChowValue CreateStr()
        {
            LookupUnary(ExpressionOp.ToStr);
            return new ChowValue(ToStr());
        }

        #endregion

        #region Comparison Operations

        internal bool IsTypeAgnosticEqualTo(ChowValue other)
        {
            switch (LookupBinary(ExpressionOp.Equal, other))
            {
                case ConversionCase.ToInt:
                    return PromoteToInt64() == other.PromoteToInt64();
                case ConversionCase.ToFloat:
                {
                    return PromoteToFloat64() == other.PromoteToFloat64();
                }
                case ConversionCase.Nothing:
                {
                    return EqualsNoConversion(other);
                }
            }

            return false;
        }

        internal bool IsNotEqualTo(ChowValue other)
        {
            switch (LookupBinary(ExpressionOp.NotEqual, other))
            {
                case ConversionCase.ToInt:
                {
                    return PromoteToInt64() != other.PromoteToInt64();
                }
                case ConversionCase.ToFloat:
                {
                    return PromoteToFloat64() != other.PromoteToFloat64();
                }
                case ConversionCase.Nothing:
                {
                    return !EqualsNoConversion(other);
                }
            }

            return true;
        }

        internal bool IsLessThan(ChowValue other)
        {
            switch (LookupBinary(ExpressionOp.Less, other))
            {
                case ConversionCase.ToInt:
                {
                    return PromoteToInt64() < other.PromoteToInt64();
                }
                case ConversionCase.ToFloat:
                {
                    return PromoteToFloat64() < other.PromoteToFloat64();
                }
                case ConversionCase.Nothing:
                {
                    if (DataType == DataType.Str && other.DataType == DataType.Str)
                    {
                        return string.CompareOrdinal(AsType<string>(), other.AsType<string>()) < 0;
                    }

                    break;
                }
            }

            throw UnsupportedBinary(ExpressionOp.Less, other);
        }

        internal bool IsGreaterThan(ChowValue other)
        {
            switch (LookupBinary(ExpressionOp.Greater, other))
            {
                case ConversionCase.ToInt:
                {
                    return PromoteToInt64() > other.PromoteToInt64();
                }
                case ConversionCase.ToFloat:
                {
                    return PromoteToFloat64() > other.PromoteToFloat64();
                }
                case ConversionCase.Nothing:
                {
                    if (DataType == DataType.Str && other.DataType == DataType.Str)
                    {
                        return string.CompareOrdinal(AsType<string>(), other.AsType<string>()) > 0;
                    }

                    break;
                }
            }

            throw UnsupportedBinary(ExpressionOp.Greater, other);
        }

        internal bool IsLessOrEqualTo(ChowValue other)
        {
            switch (LookupBinary(ExpressionOp.LessEqual, other))
            {
                case ConversionCase.ToInt:
                {
                    return PromoteToInt64() <= other.PromoteToInt64();
                }
                case ConversionCase.ToFloat:
                {
                    return PromoteToFloat64() <= other.PromoteToFloat64();
                }
                case ConversionCase.Nothing:
                {
                    if (DataType == DataType.Str && other.DataType == DataType.Str)
                    {
                        return string.CompareOrdinal(AsType<string>(), other.AsType<string>()) <= 0;
                    }

                    break;
                }
            }

            throw UnsupportedBinary(ExpressionOp.LessEqual, other);
        }

        internal bool IsGreaterOrEqualTo(ChowValue other)
        {
            switch (LookupBinary(ExpressionOp.GreaterEqual, other))
            {
                case ConversionCase.ToInt:
                {
                    return PromoteToInt64() >= other.PromoteToInt64();
                }
                case ConversionCase.ToFloat:
                {
                    return PromoteToFloat64() >= other.PromoteToFloat64();
                }
                case ConversionCase.Nothing:
                {
                    if (DataType == DataType.Str && other.DataType == DataType.Str)
                    {
                        return string.CompareOrdinal(AsType<string>(), other.AsType<string>()) >= 0;
                    }

                    break;
                }
            }

            throw UnsupportedBinary(ExpressionOp.GreaterEqual, other);
        }

        #endregion

        #region Interop

        internal ChowValue InvokeHostDelegate(ChowValue[] args)
        {
            if (DataType != DataType.Object)
            {
                throw new TypeException($"'{DataType}' object is not callable");
            }

            if (_objectValue is Func<ChowValue[], ChowValue> methodDelegate)
            {
                return methodDelegate(args ?? Array.Empty<ChowValue>());
            }

            throw new InvalidOperationException($"Object of type '{_objectValue.GetType().Name}' is not callable");
        }

        #endregion

        #region Object Overrides

        /// <summary>
        /// Strict structural equality: returns <c>true</c> only when <paramref name="obj"/> is a
        /// <see cref="ChowValue"/> of the same <see cref="DataType"/> with the same underlying value.
        /// No cross-type conversion is performed — <c>True</c> is not equal to <c>1</c>, and
        /// <c>1</c> is not equal to <c>1.0</c>. For Python <c>==</c> semantics that promote across
        /// numeric types, use <see cref="IsTypeAgnosticEqualTo"/> instead.
        /// </summary>
        /// <param name="obj">The object to compare with this instance or <c>null</c>.</param>
        /// <returns><c>true</c> if the objects are structurally equal; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return obj is ChowValue other && EqualsNoConversion(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int valueHash;

            switch (DataType)
            {
                case DataType.Int:
                {
                    valueHash = _int64Value.GetHashCode();
                    break;
                }
                case DataType.Float:
                {
                    valueHash = _float64Value.GetHashCode();
                    break;
                }
                case DataType.Bool:
                {
                    valueHash = _boolValue.GetHashCode();
                    break;
                }
                case DataType.List:
                {
                    valueHash = InternalList.ElementsHashCode((InternalList)_objectValue);
                    break;
                }
                case DataType.Dict:
                {
                    valueHash = InternalDict.ElementsHashCode((InternalDict)_objectValue);
                    break;
                }
                default:
                {
                    valueHash = _objectValue?.GetHashCode() ?? 0;
                    break;
                }
            }

            unchecked
            {
                return DataType.GetHashCode() * HASH_COMBINE_PRIME ^ valueHash;
            }
        }

        /// <summary>
        /// Returns a string representation of this instance with the same format as a Chow <b>str</b> value.
        /// </summary>
        public override string ToString()
        {
            return ToStr();
        }

        #endregion

        #region Conversion Methods

        // NOTE: These methods are for internal use only and are more performant than AsType<T>()
        // that is intended for the library's client

        internal bool ToBool()
        {
            switch (DataType)
            {
                case DataType.None:
                {
                    return NONE_REP_BOOL_VALUE;
                }
                case DataType.Bool:
                {
                    return _boolValue;
                }
                case DataType.Object:
                {
                    return OBJECT_REP_BOOL_VALUE;
                }
                case DataType.Int:
                {
                    return _int64Value != INT64_REP_BOOL_FALSE;
                }
                case DataType.Float:
                {
                    return _float64Value != FLOAT64_REP_BOOL_FALSE;
                }
                case DataType.Str:
                {
                    return StrToBool();
                }
                case DataType.List:
                {
                    return ListToBool();
                }
                case DataType.Dict:
                {
                    return DictToBool();
                }
                case DataType.Range:
                {
                    return RangeToBool();
                }
            }

            throw new InvalidOperationException();
        }

        internal long ToInt64()
        {
            switch (DataType)
            {
                case DataType.None:
                {
                    throw new InvalidOperationException("Cannot convert None to int64");
                }
                case DataType.Bool:
                {
                    return _boolValue ? BOOL_TRUE_REP_INT64 : BOOL_FALSE_REP_INT64;
                }
                case DataType.Int:
                {
                    return _int64Value;
                }
                case DataType.Float:
                {
                    return (long)_float64Value;
                }
                case DataType.Str:
                {
                    return StrToInt64();
                }
                case DataType.Object:
                {
                    throw new InvalidOperationException("Cannot convert Object to int64");
                }
                case DataType.List:
                {
                    throw new InvalidOperationException("Cannot convert List to int64");
                }
                case DataType.Dict:
                {
                    throw new InvalidOperationException("Cannot convert Dict to int64");
                }
                case DataType.Range:
                {
                    throw new InvalidOperationException("Cannot convert Range to int64");
                }
            }

            throw new InvalidOperationException();
        }

        internal double ToFloat64()
        {
            switch (DataType)
            {
                case DataType.None:
                {
                    throw new InvalidOperationException("Cannot convert None to float64");
                }
                case DataType.Bool:
                {
                    return _boolValue ? BOOL_TRUE_REP_FLOAT64 : BOOL_FALSE_REP_FLOAT64;
                }
                case DataType.Int:
                {
                    return _int64Value;
                }
                case DataType.Float:
                {
                    return _float64Value;
                }
                case DataType.Str:
                {
                    return StrToFloat64();
                }
                case DataType.Object:
                {
                    throw new InvalidOperationException("Cannot convert Object to float64");
                }
                case DataType.List:
                {
                    throw new InvalidOperationException("Cannot convert List to float64");
                }
                case DataType.Dict:
                {
                    throw new InvalidOperationException("Cannot convert Dict to float64");
                }
                case DataType.Range:
                {
                    throw new InvalidOperationException("Cannot convert Range to float64");
                }
            }

            throw new InvalidOperationException();
        }

        internal object ToObject()
        {
            switch (DataType)
            {
                case DataType.None:
                {
                    return null;
                }
                case DataType.Bool:
                {
                    return _boolValue;
                }
                case DataType.Int:
                {
                    return _int64Value;
                }
                case DataType.Float:
                {
                    return _float64Value;
                }
                case DataType.Str:
                case DataType.Object:
                case DataType.List:
                case DataType.Dict:
                case DataType.Range:
                {
                    return _objectValue;
                }
            }

            throw new InvalidOperationException();
        }

        internal string ToStr()
        {
            switch (DataType)
            {
                case DataType.None:
                {
                    return NONE_REP_STR_VALUE;
                }
                case DataType.Bool:
                {
                    return _boolValue ? BOOL_TRUE_REP_STR : BOOL_FALSE_REP_STR;
                }
                case DataType.Int:
                {
                    return _int64Value.ToString(CultureInfo.InvariantCulture);
                }
                case DataType.Float:
                {
                    return FloatToStr();
                }
                case DataType.Str:
                {
                    return StrToStr();
                }
                case DataType.Object:
                case DataType.List:
                case DataType.Dict:
                case DataType.Range:
                {
                    if (_objectValue == null)
                    {
                        // This should never happen, but we'll check just in case
                        throw new InvalidOperationException($"{nameof(ChowValue)} object with type {DataType} null");
                    }

                    return _objectValue.ToString();
                }
            }

            throw new InvalidOperationException();
        }

        #endregion

        #region Conversion Helpers

        bool StrToBool()
        {
            if (_objectValue is string strValue)
            {
                return strValue.Length != STR_LENGTH_REP_BOOL_FALSE;
            }

            throw new InvalidOperationException("Expected string value for boolean comparison");
        }

        bool ListToBool()
        {
            if (_objectValue is InternalList listValue)
            {
                return listValue.Count != LIST_COUNT_REP_BOOL_FALSE;
            }

            throw new InvalidOperationException("Expected list value for boolean comparison");
        }

        bool DictToBool()
        {
            if (_objectValue is InternalDict dictValue)
            {
                return dictValue.Count != DICT_COUNT_REP_BOOL_FALSE;
            }

            throw new InvalidOperationException("Expected dict value for boolean comparison");
        }

        bool RangeToBool()
        {
            if (_objectValue is InternalRange rangeValue)
            {
                return rangeValue.Count != RANGE_COUNT_REP_BOOL_FALSE;
            }

            throw new InvalidOperationException("Expected range value for boolean comparison");
        }

        long StrToInt64()
        {
            if (!(_objectValue is string strValue))
            {
                throw new InvalidOperationException("Expected string value for int64 conversion");
            }

            if (long.TryParse(strValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt64))
            {
                return parsedInt64;
            }

            throw new InvalidOperationException($"Cannot convert string '{strValue}' to int64");
        }

        double StrToFloat64()
        {
            if (!(_objectValue is string strValue))
            {
                throw new InvalidOperationException("Expected string value for float64 conversion");
            }

            if (double.TryParse(strValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedFloat64))
            {
                return parsedFloat64;
            }

            throw new InvalidOperationException($"Cannot convert string '{strValue}' to float64");
        }

        string FloatToStr()
        {
            var formatted = _float64Value.ToString(CultureInfo.InvariantCulture);

            if (IsFractionalSuffix(formatted))
            {
                formatted += FLOAT64_INTEGER_FRACTIONAL_SUFFIX;
            }

            return formatted;
        }

        static bool IsFractionalSuffix(string formatted)
        {
            return formatted.IndexOf(FLOAT64_DECIMAL_POINT_CHAR) == CHAR_NOT_FOUND_INDEX
                && formatted.IndexOf(FLOAT64_EXPONENT_LOWER_CHAR) == CHAR_NOT_FOUND_INDEX
                && formatted.IndexOf(FLOAT64_EXPONENT_UPPER_CHAR) == CHAR_NOT_FOUND_INDEX;
        }

        string StrToStr()
        {
            if (_objectValue is string strValue)
            {
                return strValue;
            }

            throw new InvalidOperationException("Expected string value for string conversion");
        }

        #endregion

        #region Operator Dispatch Helpers

        // Promotion-rule lookup and result-coercion helpers used by the arithmetic/comparison instance
        // methods above. Operand promotion is intentionally limited to the three numeric tags
        // (Bool/Int/Float); the map guarantees ToInt/ToFloat is only reported for those.

        ConversionCase LookupBinary(ExpressionOp op, ChowValue right)
        {
            return DataTypeConversionMap.GetLeftRightConversionCase(op, DataType, right.DataType);
        }

        ConversionCase LookupUnary(ExpressionOp op)
        {
            return DataTypeConversionMap.GetOperandConversionCase(op, DataType);
        }

        TypeException UnsupportedBinary(ExpressionOp op, ChowValue right)
        {
            return new TypeException($"unsupported operand type(s) for {op}: '{DataType}' and '{right.DataType}'");
        }

        TypeException UnsupportedUnary(ExpressionOp op)
        {
            return new TypeException($"bad operand type for unary {op}: '{DataType}'");
        }

        // TODO: These are redundant, the ToX methods should be used instead.
        long PromoteToInt64()
        {
            switch (DataType)
            {
                case DataType.Bool:
                {
                    return _boolValue ? 1L : 0L;
                }
                case DataType.Int:
                {
                    return _int64Value;
                }
                default:
                {
                    throw new InvalidOperationException($"Cannot promote {DataType} to int");
                }
            }
        }

        double PromoteToFloat64()
        {
            switch (DataType)
            {
                case DataType.Bool:
                {
                    return _boolValue ? 1.0 : 0.0;
                }
                case DataType.Int:
                {
                    return _int64Value;
                }
                case DataType.Float:
                {
                    return _float64Value;
                }
                default:
                {
                    throw new InvalidOperationException($"Cannot promote {DataType} to float");
                }
            }
        }

        // Equality fallback used when DataTypeConversionMap reports Nothing for ==/!= operands.
        // Cross-type combinations are never equal (Python: 1 == "1" → False); same-type combinations
        // delegate to the underlying value's identity/structural equality.
        bool EqualsNoConversion(ChowValue other)
        {
            if (DataType != other.DataType)
            {
                return false;
            }

            switch (DataType)
            {
                case DataType.None:
                {
                    return true;
                }
                case DataType.Bool:
                {
                    return _boolValue == other._boolValue;
                }
                case DataType.Int:
                {
                    return _int64Value == other._int64Value;
                }
                case DataType.Float:
                {
                    return _float64Value.Equals(other._float64Value);
                }
                case DataType.Str:
                {
                    return (string)_objectValue == (string)other._objectValue;
                }
                case DataType.List:
                {
                    return InternalList.ElementsEqual((InternalList)_objectValue, (InternalList)other._objectValue);
                }
                case DataType.Dict:
                {
                    return InternalDict.ElementsEqual((InternalDict)_objectValue, (InternalDict)other._objectValue);
                }
                case DataType.Range:
                case DataType.Object:
                {
                    return ReferenceEquals(_objectValue, other._objectValue);
                }
                default:
                {
                    return false;
                }
            }
        }

        static string RepeatString(string source, int count)
        {
            if (count <= 0 || source.Length == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder(source.Length * count);

            for (var index = 0; index < count; index++)
            {
                builder.Append(source);
            }

            return builder.ToString();
        }

        // Bool is treated as a subtype of Int for container-repeat dispatch (Python parity).
        static bool IsIntegerTag(DataType dataType)
        {
            return dataType == DataType.Int || dataType == DataType.Bool;
        }

        // TODO: Make it so an overflow throws an exception instead of wrapping silently.
        // Exponent-by-squaring. Caller guarantees exponent >= 0 (negative exponents are promoted to
        // float by CreatePower before this is reached). Overflow wraps silently.
        static long IntPow(long b, long e)
        {
            var result = 1L;

            while (e > 0L)
            {
                if ((e & 1L) == 1L)
                {
                    result *= b;
                }

                b *= b;
                e >>= 1;
            }

            return result;
        }

        #endregion

        #region Constants

        // GetHashCode mixing multiplier (standard small prime for combining hashes)
        const int HASH_COMBINE_PRIME = 397;

        // Constructor defaults
        const bool DEFAULT_BOOL_VALUE = false;
        const object DEFAULT_OBJECT_VALUE = null;
        const long DEFAULT_INT64_VALUE = 0L;
        const double DEFAULT_FLOAT64_VALUE = 0.0;

        // ToBool source representations (numeric "false" values)
        const long INT64_REP_BOOL_FALSE = 0L;
        const double FLOAT64_REP_BOOL_FALSE = 0.0;

        // ToBool source representations (container/string "false" lengths, plus None/Object fixed reps)
        const int STR_LENGTH_REP_BOOL_FALSE = 0;
        const int LIST_COUNT_REP_BOOL_FALSE = 0;
        const int DICT_COUNT_REP_BOOL_FALSE = 0;
        const int RANGE_COUNT_REP_BOOL_FALSE = 0;
        const bool NONE_REP_BOOL_VALUE = false;
        const bool OBJECT_REP_BOOL_VALUE = true;

        // ToInt64 source representations (bool -> int64)
        const long BOOL_FALSE_REP_INT64 = 0L;
        const long BOOL_TRUE_REP_INT64 = 1L;

        // ToFloat64 source representations (bool -> float64)
        const double BOOL_FALSE_REP_FLOAT64 = 0.0;
        const double BOOL_TRUE_REP_FLOAT64 = 1.0;

        // ToStr source representations (None/bool -> str)
        const string NONE_REP_STR_VALUE = "None";
        const string BOOL_FALSE_REP_STR = "False";
        const string BOOL_TRUE_REP_STR = "True";

        // ToStr float formatting (append ".0" when ToString output has no decimal point or exponent)
        const string FLOAT64_INTEGER_FRACTIONAL_SUFFIX = ".0";
        const char FLOAT64_DECIMAL_POINT_CHAR = '.';
        const char FLOAT64_EXPONENT_LOWER_CHAR = 'e';
        const char FLOAT64_EXPONENT_UPPER_CHAR = 'E';

        // String.IndexOf "not found" sentinel (used by ToStr float formatting check)
        const int CHAR_NOT_FOUND_INDEX = -1;

        #endregion

    }
}
