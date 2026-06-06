using System;
using System.Runtime.InteropServices;
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
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct RuntimeValue
    {
        // TODO: Major refactor going on currently
        public static readonly RuntimeValue None = new RuntimeValue(DataType.None);
        static readonly Dictionary<Type, DataType> DataTypeMap = new Dictionary<Type, DataType>
        {
            { typeof(bool), DataType.Bool },
            { typeof(long), DataType.Long },
            { typeof(int), DataType.Long },
            { typeof(double), DataType.Double },
            { typeof(string), DataType.Str },
            { typeof(SourceDictionary), DataType.Dict },
            { typeof(SourceRange), DataType.Range },
            { typeof(SourceList), DataType.List }
        };

        #region Fields
        
        /// <summary>Represents the RuntimeValue equivalent to null/nil/none values.</summary>
        [FieldOffset(OBJ_FIELD_OFFSET)] readonly object _obj;
        [FieldOffset(LONG_FIELD_OFFSET)] readonly long _long;
        [FieldOffset(DBL_FIELD_OFFSET)] readonly double _dbl;
        [FieldOffset(TAG_FIELD_OFFSET)] readonly DataType _dataType;

        #endregion

        #region Properties

        bool BoolValue => _long == BOOL_T_TO_LONG;

        internal DataType DataType => _dataType;
        #endregion

        #region Constructors

        RuntimeValue(
            DataType dataType = DataType.None,
            bool boolValue = NOT_BOOL_INIT,
            object objVal = NOT_OBJ_INIT,
            long longVal = NOT_LONG_INIT,
            double doubleVal = NOT_DBL_INIT)
        {
            _dataType = dataType;
            _obj = objVal;

            // _long and _dbl share the same FieldOffset (explicit-layout union). The compiler still
            // requires both to be definitely assigned, so every branch sets the live field to its
            // value and the dead field to its default — each field is written exactly once.
            switch (dataType)
            {
                case DataType.Bool:
                    _dbl = NOT_DBL_INIT;
                    _long = boolValue ? BOOL_T_TO_LONG : BOOL_F_TO_LONG;
                    break;
                case DataType.Double:
                    _long = NOT_LONG_INIT;
                    _dbl = doubleVal;
                    break;
                case DataType.Object:
                case DataType.List:
                case DataType.Dict:
                case DataType.Range:
                case DataType.Str:
                case DataType.None:
                case DataType.Long:
                    _dbl = NOT_DBL_INIT;
                    _long = longVal;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
            }
        }

        internal RuntimeValue(long value) : this(DataType.Long, longVal: value) {}

        internal RuntimeValue(double val) : this(DataType.Double, doubleVal: val) {}

        internal RuntimeValue(bool value) : this(DataType.Bool, value) {}

        internal RuntimeValue(string value) : this(DataType.Str, objVal: value) {}

        internal RuntimeValue(SourceList list) : this(DataType.List, objVal: list) {}

        internal RuntimeValue(SourceDictionary dictionary) : this(DataType.Dict, objVal: dictionary) {}

        internal RuntimeValue(SourceRange range) : this(DataType.Range, objVal: range) {}

        /// <summary>
        /// Resolves and converts the value of <paramref name="obj"/> and initializes instance with
        /// the converted value (if a dataType is defined for that type).
        /// </summary>
        internal RuntimeValue(object obj)
        {
            switch (obj)
            {
                case null:
                // TODO: Look into changing this to RuntimeValue.None.
                    throw new ArgumentNullException(nameof(obj));
                case string strValue:
                    this = new RuntimeValue(strValue);
                    break;
                case long longValue:
                    this = new RuntimeValue(longValue);
                    break;
                case int intValue:
                    this = new RuntimeValue(intValue);
                    break;
                case double doubleValue:
                    this = new RuntimeValue(doubleValue);
                    break;
                case bool boolValue:
                    this = new RuntimeValue(boolValue);
                    break;
                case SourceList listValue:
                    this = new RuntimeValue(listValue);
                    break;
                case SourceDictionary dictValue:
                    this = new RuntimeValue(dictValue);
                    break;
                case SourceRange rangeValue:
                    this = new RuntimeValue(rangeValue);
                    break;
                case RuntimeValue chowValue:
                    // **IMPORTANT**: CHOW VALUES ARE NEVER DIRECTLY WRAPPED IN OTHER CHOW VALUE INSTANCES
                    this = chowValue;
                    break;
                default:
                    _dataType = DataType.Object;
                    _obj = obj;
                    _long = NOT_LONG_INIT;
                    _dbl = NOT_DBL_INIT;
                    break;
            }
        }

        #endregion

        #region Type API
        // WARNING: THIS METHOD WILL BE REMOVED IN FUTURE REFACTOR!
        // DO NOT USE ANY OF THESE METHODS IN NEW CLASSES OR ADD THEM TO PRE-EXISTING ONES

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
                if (_obj is TDataType typedObject)
                {
                    return typedObject;
                }

                throw new InvalidOperationException($"Cannot convert {_dataType} to {typeOf}");
            }

            switch (targetDataType)
            {
                case DataType.Bool:
                {
                    return (TDataType)(object)ToBool();
                }
                case DataType.Long:
                {
                    // The map aliases both typeof(long) and typeof(int) to DataType.Long.
                    // For T == int we truncate; for T == long we return the full 64-bit value.
                    if (typeOf == typeof(int))
                    {
                        // TODO: Add error checking for overflow scenarios
                        return (TDataType)(object)(int)ToLong();
                    }

                    return (TDataType)(object)ToLong();
                }
                case DataType.Double:
                {
                    return (TDataType)(object)ToDouble();
                }
                case DataType.Str:
                {
                    return (TDataType)(object)ToString();
                }
                case DataType.List:
                case DataType.Dict:
                case DataType.Range:
                {
                    if (_obj is TDataType typedObject)
                    {
                        return typedObject;
                    }

                    break;
                }
            }

            throw new InvalidOperationException($"Cannot convert {_dataType} to {typeof(TDataType)}");
        }
        
        #endregion

        #region Arithmetic & Logic Operations
        // WARNING: THESE METHODS WILL BE REMOVED IN FUTURE REFACTOR!
        // DO NOT USE ANY OF THESE METHODS OUTSIDE OF THE VirtualMachine CLASS

        // Instance methods to avoid passing two ChowValues as parameters. Each returns a new RuntimeValue
        // (the struct is readonly, so no risk of accidentally mutating this instance's internal state).
        // Promotion rules come from DataTypeConversionMap (the single source of truth). Carve-outs for
        // container/string ops (list+list, list*int, str+str, str*int, dict|dict) are dispatched when
        // the map reports ConversionCase.Nothing.

        internal RuntimeValue CreateSum(RuntimeValue rightOperand)
        {
            switch (LookupBinary(ExpressionOperator.Add, rightOperand))
            {
                case ConversionCase.ToInt:
                {
                    return new RuntimeValue(PromoteToLong() + rightOperand.PromoteToLong());
                }
                case ConversionCase.ToFloat:
                {
                    return new RuntimeValue(PromoteToDouble() + rightOperand.PromoteToDouble());
                }
                case ConversionCase.Nothing:
                {
                    if (_dataType == DataType.List && rightOperand._dataType == DataType.List)
                    {
                        return new RuntimeValue(SourceList.Concat(AsType<SourceList>(), rightOperand.AsType<SourceList>()));
                    }

                    if (_dataType == DataType.Str && rightOperand._dataType == DataType.Str)
                    {
                        return new RuntimeValue(AsType<string>() + rightOperand.AsType<string>());
                    }

                    break;
                }
            }

            throw UnsupportedBinary(ExpressionOperator.Add, rightOperand);
        }

        internal RuntimeValue CreateDifference(RuntimeValue rightOperand)
        {
            if (LookupBinary(ExpressionOperator.Subtract, rightOperand) == ConversionCase.ToInt)
            {
                return new RuntimeValue(PromoteToLong() - rightOperand.PromoteToLong());
            }

            if (LookupBinary(ExpressionOperator.Subtract, rightOperand) == ConversionCase.ToFloat)
            {
                return new RuntimeValue(PromoteToDouble() - rightOperand.PromoteToDouble());
            }

            if (LookupBinary(ExpressionOperator.Subtract, rightOperand) == ConversionCase.Nothing)
            {
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }

            throw UnsupportedBinary(ExpressionOperator.Subtract, rightOperand);
        }

        internal RuntimeValue CreateProduct(RuntimeValue rightOperand)
        {
            switch (LookupBinary(ExpressionOperator.Multiply, rightOperand))
            {
                case ConversionCase.ToInt:
                {
                    return new RuntimeValue(PromoteToLong() * rightOperand.PromoteToLong());
                }
                case ConversionCase.ToFloat:
                {
                    return new RuntimeValue(PromoteToDouble() * rightOperand.PromoteToDouble());
                }
                case ConversionCase.Nothing:
                {
                    // Python treats bool as a subtype of int, so [1] * True and "ab" * True are valid.
                    if (_dataType == DataType.List && IsIntegerTag(rightOperand._dataType))
                    {
                        return new RuntimeValue(SourceList.Repeat(AsType<SourceList>(), rightOperand.AsType<int>()));
                    }

                    if (IsIntegerTag(_dataType) && rightOperand._dataType == DataType.List)
                    {
                        return new RuntimeValue(SourceList.Repeat(rightOperand.AsType<SourceList>(), AsType<int>()));
                    }

                    if (_dataType == DataType.Str && IsIntegerTag(rightOperand._dataType))
                    {
                        return new RuntimeValue(RepeatString(AsType<string>(), rightOperand.AsType<int>()));
                    }

                    if (IsIntegerTag(_dataType) && rightOperand._dataType == DataType.Str)
                    {
                        return new RuntimeValue(RepeatString(rightOperand.AsType<string>(), AsType<int>()));
                    }

                    break;
                }
            }

            throw UnsupportedBinary(ExpressionOperator.Multiply, rightOperand);
        }

        internal RuntimeValue CreateQuotient(RuntimeValue rightOperand)
        {
            // Python semantics: `/` always produces a float, even for int / int.
            switch (LookupBinary(ExpressionOperator.Divide, rightOperand))
            {
                case ConversionCase.ToFloat:
                {
                    var divisor = rightOperand.PromoteToDouble();

                    return divisor == 0.0 
                        ? throw new ZeroDivisionException() 
                        : new RuntimeValue(PromoteToDouble() / divisor);

                }
                case ConversionCase.Nothing:
                case ConversionCase.ToInt:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            throw UnsupportedBinary(ExpressionOperator.Divide, rightOperand);
        }

        internal RuntimeValue CreateModulus(RuntimeValue rightOperand)
        {
            // Python semantics: result has the sign of the divisor.
            switch (LookupBinary(ExpressionOperator.Modulus, rightOperand))
            {
                case ConversionCase.ToInt:
                {
                    var a = PromoteToLong();
                    var b = rightOperand.PromoteToLong();

                    if (b == 0L)
                    {
                        throw new ZeroDivisionException();
                    }

                    return new RuntimeValue((a % b + b) % b);
                }
                case ConversionCase.ToFloat:
                {
                    var l = PromoteToDouble();
                    var r = rightOperand.PromoteToDouble();

                    if (r == 0.0)
                    {
                        throw new ZeroDivisionException();
                    }

                    return new RuntimeValue((l % r + r) % r);
                }
            }

            throw UnsupportedBinary(ExpressionOperator.Modulus, rightOperand);
        }

        internal RuntimeValue CreateFloorQuotient(RuntimeValue rightOperand)
        {
            // Python semantics: floors toward negative infinity. Integer path stays in longs (no detour
            // through double) so values past 2^53 remain exact.
            switch (LookupBinary(ExpressionOperator.FloorDivide, rightOperand))
            {
                case ConversionCase.ToInt:
                {
                    var a = PromoteToLong();
                    var b = rightOperand.PromoteToLong();

                    if (b == 0L)
                    {
                        throw new ZeroDivisionException();
                    }

                    var q = a / b;

                    if (a % b != 0L && a < 0L != b < 0L)
                    {
                        q--;
                    }

                    return new RuntimeValue(q);
                }
                case ConversionCase.ToFloat:
                {
                    var divisor = rightOperand.PromoteToDouble();

                    if (divisor == 0.0)
                    {
                        throw new ZeroDivisionException();
                    }

                    return new RuntimeValue(Math.Floor(PromoteToDouble() / divisor));
                }
            }

            throw UnsupportedBinary(ExpressionOperator.FloorDivide, rightOperand);
        }

        internal RuntimeValue CreatePower(RuntimeValue rightOperand)
        {
            // Python semantics: float if either operand is float, or if exponent is negative.
            // This is the one documented map override: the negative-exponent rule is value-dependent
            // (depends on the runtime exponent's sign), not type-dependent, so it cannot live in the
            // type-keyed map. Every other dispatch path defers to DataTypeConversionMap.
            var conv = LookupBinary(ExpressionOperator.Exponentiate, rightOperand);

            if (conv == ConversionCase.ToInt && rightOperand.PromoteToLong() < 0)
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
                    return new RuntimeValue(IntPow(PromoteToLong(), rightOperand.PromoteToLong()));
                }
                case ConversionCase.ToFloat:
                {
                    return new RuntimeValue(Math.Pow(PromoteToDouble(), rightOperand.PromoteToDouble()));
                }
            }

            throw UnsupportedBinary(ExpressionOperator.Exponentiate, rightOperand);
        }

        internal RuntimeValue CreateUnion(RuntimeValue rightOperand)
        {
            if (LookupBinary(ExpressionOperator.BinaryOr, rightOperand) == ConversionCase.Nothing
                && _dataType == DataType.Dict && rightOperand._dataType == DataType.Dict)
            {
                return new RuntimeValue(SourceDictionary.Merge(AsType<SourceDictionary>(), rightOperand.AsType<SourceDictionary>()));
            }

            throw UnsupportedBinary(ExpressionOperator.BinaryOr, rightOperand);
        }

        internal RuntimeValue CreateNegation()
        {
            switch (LookupUnary(ExpressionOperator.Negate))
            {
                case ConversionCase.ToInt:
                {
                    return new RuntimeValue(-PromoteToLong());
                }
                case ConversionCase.ToFloat:
                {
                    return new RuntimeValue(-PromoteToDouble());
                }
            }

            throw UnsupportedUnary(ExpressionOperator.Negate);
        }

        internal RuntimeValue CreateLogicalNot()
        {
            // The map records this as Nothing for every type; consult it for consistency and so that
            // a future map change (e.g. restricting unary `not` to specific types) propagates here.
            LookupUnary(ExpressionOperator.Not);
            return new RuntimeValue(!ToBool());
        }

        internal RuntimeValue CreateStr()
        {
            LookupUnary(ExpressionOperator.ToStr);
            return new RuntimeValue(ToString());
        }

        #endregion

        #region Comparison Operations
        // WARNING: THESE METHODS WILL BE REMOVED IN FUTURE REFACTOR!
        // DO NOT USE ANY OF THESE METHODS IN NEW CLASSES OR ADD THEM TO PRE-EXISTING ONES

        internal bool IsTypeAgnosticEqualTo(RuntimeValue other)
        {
            switch (LookupBinary(ExpressionOperator.Equal, other))
            {
                case ConversionCase.ToInt:
                    return PromoteToLong() == other.PromoteToLong();
                case ConversionCase.ToFloat:
                {
                    return PromoteToDouble() == other.PromoteToDouble();
                }
                case ConversionCase.Nothing:
                {
                    return EqualsNoConversion(other);
                }
            }

            return false;
        }

        internal bool IsNotEqualTo(RuntimeValue other)
        {
            switch (LookupBinary(ExpressionOperator.NotEqual, other))
            {
                case ConversionCase.ToInt:
                {
                    return PromoteToLong() != other.PromoteToLong();
                }
                case ConversionCase.ToFloat:
                {
                    return PromoteToDouble() != other.PromoteToDouble();
                }
                case ConversionCase.Nothing:
                {
                    return !EqualsNoConversion(other);
                }
            }

            return true;
        }

        internal bool IsLessThan(RuntimeValue other)
        {
            switch (LookupBinary(ExpressionOperator.Less, other))
            {
                case ConversionCase.ToInt:
                {
                    return PromoteToLong() < other.PromoteToLong();
                }
                case ConversionCase.ToFloat:
                {
                    return PromoteToDouble() < other.PromoteToDouble();
                }
                case ConversionCase.Nothing:
                {
                    if (_dataType == DataType.Str && other._dataType == DataType.Str)
                    {
                        return string.CompareOrdinal(AsType<string>(), other.AsType<string>()) < 0;
                    }

                    break;
                }
            }

            throw UnsupportedBinary(ExpressionOperator.Less, other);
        }

        internal bool IsGreaterThan(RuntimeValue other)
        {
            switch (LookupBinary(ExpressionOperator.Greater, other))
            {
                case ConversionCase.ToInt:
                {
                    return PromoteToLong() > other.PromoteToLong();
                }
                case ConversionCase.ToFloat:
                {
                    return PromoteToDouble() > other.PromoteToDouble();
                }
                case ConversionCase.Nothing:
                {
                    if (_dataType == DataType.Str && other._dataType == DataType.Str)
                    {
                        return string.CompareOrdinal(AsType<string>(), other.AsType<string>()) > 0;
                    }

                    break;
                }
            }

            throw UnsupportedBinary(ExpressionOperator.Greater, other);
        }

        internal bool IsLessOrEqualTo(RuntimeValue other)
        {
            switch (LookupBinary(ExpressionOperator.LessEqual, other))
            {
                case ConversionCase.ToInt:
                {
                    return PromoteToLong() <= other.PromoteToLong();
                }
                case ConversionCase.ToFloat:
                {
                    return PromoteToDouble() <= other.PromoteToDouble();
                }
                case ConversionCase.Nothing:
                {
                    if (_dataType == DataType.Str && other._dataType == DataType.Str)
                    {
                        return string.CompareOrdinal(AsType<string>(), other.AsType<string>()) <= 0;
                    }

                    break;
                }
            }

            throw UnsupportedBinary(ExpressionOperator.LessEqual, other);
        }

        internal bool IsGreaterOrEqualTo(RuntimeValue other)
        {
            switch (LookupBinary(ExpressionOperator.GreaterEqual, other))
            {
                case ConversionCase.ToInt:
                {
                    return PromoteToLong() >= other.PromoteToLong();
                }
                case ConversionCase.ToFloat:
                {
                    return PromoteToDouble() >= other.PromoteToDouble();
                }
                case ConversionCase.Nothing:
                {
                    if (_dataType == DataType.Str && other._dataType == DataType.Str)
                    {
                        return string.CompareOrdinal(AsType<string>(), other.AsType<string>()) >= 0;
                    }

                    break;
                }
            }

            throw UnsupportedBinary(ExpressionOperator.GreaterEqual, other);
        }

        #endregion

        #region Interop

        internal RuntimeValue InvokeHostDelegate(RuntimeValue[] args)
        {
            if (_dataType != DataType.Object)
            {
                throw new DataTypeException($"'{_dataType}' object is not callable");
            }

            if (_obj is Func<RuntimeValue[], RuntimeValue> methodDelegate)
            {
                return methodDelegate(args ?? Array.Empty<RuntimeValue>());
            }

            throw new InvalidOperationException($"Object of type '{_obj.GetType().Name}' is not callable");
        }

        #endregion

        #region Object Overrides

        /// <summary>
        /// Strict structural equality: returns <c>true</c> only when <paramref name="obj"/> is a
        /// <see cref="RuntimeValue"/> of the same <see cref="_dataType"/> with the same underlying value.
        /// No cross-type conversion is performed — <c>True</c> is not equal to <c>1</c>, and
        /// <c>1</c> is not equal to <c>1.0</c>. For Python <c>==</c> semantics that promote across
        /// numeric types, use <see cref="IsTypeAgnosticEqualTo"/> instead.
        /// </summary>
        /// <param name="obj">The object to compare with this instance or <c>null</c>.</param>
        /// <returns><c>true</c> if the objects are structurally equal; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return obj is RuntimeValue other && EqualsNoConversion(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int valueHash;

            switch (_dataType)
            {
                case DataType.Long:
                {
                    valueHash = _long.GetHashCode();
                    break;
                }
                case DataType.Double:
                {
                    valueHash = _dbl.GetHashCode();
                    break;
                }
                case DataType.Bool:
                {
                    valueHash = BoolValue.GetHashCode();
                    break;
                }
                case DataType.List:
                {
                    valueHash = SourceList.ElementsHashCode((SourceList)_obj);
                    break;
                }
                case DataType.Dict:
                {
                    valueHash = SourceDictionary.ElementsHashCode((SourceDictionary)_obj);
                    break;
                }
                default:
                {
                    valueHash = _obj?.GetHashCode() ?? 0;
                    break;
                }
            }

            unchecked
            {
                return _dataType.GetHashCode() * HASH_COMBINE_PRIME ^ valueHash;
            }
        }

        #endregion

        #region Conversion Methods

        // NOTE: These methods are for internal use only and are more performant than AsType<T>()
        // that is intended for the library's client

        internal bool ToBool()
        {
            switch (_dataType)
            {
                case DataType.None:
                    return NONE_TO_BOOL_T_AND_F;

                case DataType.Bool:
                    return BoolValue;

                case DataType.Object:
                    return OBJ_TO_BOOL_T_AND_F;

                case DataType.Long:
                    return _long != LONG_TO_BOOL_F;

                case DataType.Double:
                    return Math.Abs(_dbl - DBL_TO_BOOL_F) > TOLERANCE;

                case DataType.Str:
                    return StrToBool();

                case DataType.List:
                    return ListToBool();

                case DataType.Dict:
                    return DictToBool();
                    
                case DataType.Range:
                    return RangeToBool();

                default:
                    throw new DataTypeException(GetConversionErrorMessage(_dataType, DataType.Bool));
            }
        }

        const double TOLERANCE = 0.000000000000001;

        internal long ToLong()
        {
            switch (_dataType)
            {
                case DataType.Bool:
                    return BoolValue ? BOOL_T_TO_LONG : BOOL_F_TO_LONG;
                    
                case DataType.Long:
                    return _long;
                    
                case DataType.Double:
                    return (long)_dbl;
                    
                case DataType.Str:
                    return StrToLong();
                    
                default:
                    throw new DataTypeException(GetConversionErrorMessage(_dataType, DataType.Long));
            }

        }

        internal double ToDouble()
        {
            switch (_dataType)
            {
                case DataType.Bool:
                    return BoolValue ? BOOL_T_TO_DBL : BOOL_F_TO_DBL;

                case DataType.Long:
                    return _long;

                case DataType.Double:
                    return _dbl;

                case DataType.Str:
                    return StrToDouble();
                    
                default:
                    throw new DataTypeException(GetConversionErrorMessage(_dataType, DataType.Double));
            }
        }

        internal object ToObject()
        {
            switch (_dataType)
            {
                case DataType.None:
                    return null;
                    
                case DataType.Bool:
                    return BoolValue;
                    
                case DataType.Long:
                    return _long;
                    
                case DataType.Double:
                    return _dbl;

                default:
                    return _obj;
            }
        }
        
        public override string ToString()
        {
            switch (_dataType)
            {
                case DataType.None:
                    // TODO: Update this class to use DataTypeNames where literals or consts are used
                    return NONE_TO_STR;
                    
                case DataType.Bool:
                    return BoolValue ? BOOL_T_TO_STR : BOOL_F_TO_STR;
                    
                case DataType.Long:
                    return _long.ToString(CultureInfo.InvariantCulture);
                    
                case DataType.Double:
                    return FloatToStr();

                default:
                    return _obj.ToString();
            }
        }

        static string GetConversionErrorMessage(DataType fromDataType, DataType toDataType)
        {
            return $"Cannot convert {DataTypeNames.GetTypeName(fromDataType)} to {DataTypeNames.GetTypeName(toDataType)}";
        }

        #endregion

        #region Conversion Helpers

        bool StrToBool()
        {
            if (_obj is string strValue)
            {
                return strValue.Length != STR_LEN_TO_BOOL_F;
            }

            throw new InvalidOperationException("Expected string value for boolean comparison");
        }

        bool ListToBool()
        {
            if (_obj is SourceList listValue)
            {
                return listValue.Count != LIST_COUNT_TO_BOOL_F;
            }

            throw new InvalidOperationException("Expected list value for boolean comparison");
        }

        bool DictToBool()
        {
            if (_obj is SourceDictionary dictValue)
            {
                return dictValue.Count != DICT_LEN_TO_BOOL_F;
            }

            throw new InvalidOperationException("Expected dict value for boolean comparison");
        }

        bool RangeToBool()
        {
            if (_obj is SourceRange rangeValue)
            {
                return rangeValue.Count != RNG_LEN_TO_BOOL_F;
            }

            throw new InvalidOperationException("Expected range value for boolean comparison");
        }

        long StrToLong()
        {
            if (!(_obj is string strValue))
            {
                throw new InvalidOperationException("Expected string value for long conversion");
            }

            if (long.TryParse(strValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedLong))
            {
                return parsedLong;
            }

            throw new InvalidOperationException($"Cannot convert string '{strValue}' to long");
        }

        double StrToDouble()
        {
            if (!(_obj is string strValue))
            {
                throw new InvalidOperationException("Expected string value for double conversion");
            }

            if (double.TryParse(strValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedDouble))
            {
                return parsedDouble;
            }

            throw new InvalidOperationException($"Cannot convert string '{strValue}' to double");
        }

        string FloatToStr()
        {
            var formatted = _dbl.ToString(CultureInfo.InvariantCulture);

            if (IsFractionalSuffix(formatted))
            {
                formatted += DBL_LONG_FRACTION_SUFFIX;
            }

            return formatted;
        }

        static bool IsFractionalSuffix(string formatted)
        {
            return formatted.IndexOf(DBL_POINT_CHAR) == CHAR_NOT_FOUND_INX
                && formatted.IndexOf(DBL_POW_LOWER_CHAR) == CHAR_NOT_FOUND_INX
                && formatted.IndexOf(DBL_POW_UPPER_CHAR) == CHAR_NOT_FOUND_INX;
        }

        #endregion

        #region Operator Dispatch Helpers

        // Promotion-rule lookup and result-coercion helpers used by the arithmetic/comparison instance
        // methods above. Operand promotion is intentionally limited to the three numeric tags
        // (Bool/Long/Double); the map guarantees ToInt/ToFloat is only reported for those.

        ConversionCase LookupBinary(ExpressionOperator @operator, RuntimeValue right)
        {
            return DataTypeConversionMap.GetLeftRightConversionCase(@operator, _dataType, right._dataType);
        }

        ConversionCase LookupUnary(ExpressionOperator @operator)
        {
            return DataTypeConversionMap.GetOperandConversionCase(@operator, _dataType);
        }

        DataTypeException UnsupportedBinary(ExpressionOperator @operator, RuntimeValue right)
        {
            return new DataTypeException($"unsupported operand type(s) for {@operator}: '{_dataType}' and '{right._dataType}'");
        }

        DataTypeException UnsupportedUnary(ExpressionOperator @operator)
        {
            return new DataTypeException($"bad operand type for unary {@operator}: '{_dataType}'");
        }

        // TODO: These are redundant, the ToX methods should be used instead.
        long PromoteToLong()
        {
            switch (_dataType)
            {
                case DataType.Bool:
                {
                    return BoolValue ? 1L : 0L;
                }
                case DataType.Long:
                {
                    return _long;
                }
                default:
                {
                    throw new InvalidOperationException($"Cannot promote {_dataType} to int");
                }
            }
        }

        double PromoteToDouble()
        {
            switch (_dataType)
            {
                case DataType.Bool:
                {
                    return BoolValue ? 1.0 : 0.0;
                }
                case DataType.Long:
                {
                    return _long;
                }
                case DataType.Double:
                {
                    return _dbl;
                }
                default:
                {
                    throw new InvalidOperationException($"Cannot promote {_dataType} to float");
                }
            }
        }

        // Equality fallback used when DataTypeConversionMap reports Nothing for ==/!= operands.
        // Cross-type combinations are never equal (Python: 1 == "1" → False); same-type combinations
        // delegate to the underlying value's identity/structural equality.
        bool EqualsNoConversion(RuntimeValue other)
        {
            if (_dataType != other._dataType)
            {
                return false;
            }

            switch (_dataType)
            {
                case DataType.None:
                {
                    return true;
                }
                case DataType.Bool:
                {
                    return BoolValue == other.BoolValue;
                }
                case DataType.Long:
                {
                    return _long == other._long;
                }
                case DataType.Double:
                {
                    return _dbl.Equals(other._dbl);
                }
                case DataType.Str:
                {
                    return (string)_obj == (string)other._obj;
                }
                case DataType.List:
                {
                    return SourceList.ElementsEqual((SourceList)_obj, (SourceList)other._obj);
                }
                case DataType.Dict:
                {
                    return SourceDictionary.ElementsEqual((SourceDictionary)_obj, (SourceDictionary)other._obj);
                }
                case DataType.Range:
                case DataType.Object:
                {
                    return ReferenceEquals(_obj, other._obj);
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

        // Bool is treated as a subtype of Long for container-repeat dispatch (Python parity).
        static bool IsIntegerTag(DataType dataType)
        {
            return dataType == DataType.Long || dataType == DataType.Bool;
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
        const bool NOT_BOOL_INIT = false;
        const object NOT_OBJ_INIT = null;
        const long NOT_LONG_INIT = 0L;
        const double NOT_DBL_INIT = 0.0;

        // ToBool source representations (numeric "false" values)
        const long LONG_TO_BOOL_F = 0L;
        const double DBL_TO_BOOL_F = 0.0;

        // ToBool source representations (container/string "false" lengths, plus None/Object fixed reps)
        const int STR_LEN_TO_BOOL_F = 0;
        const int LIST_COUNT_TO_BOOL_F = 0;
        const int DICT_LEN_TO_BOOL_F = 0;
        const int RNG_LEN_TO_BOOL_F = 0;
        const bool NONE_TO_BOOL_T_AND_F = false;
        const bool OBJ_TO_BOOL_T_AND_F = true;

        // ToLong source representations (bool -> long)
        const long BOOL_F_TO_LONG = 0L;
        const long BOOL_T_TO_LONG = 1L;

        // ToDouble source representations (bool -> double)
        const double BOOL_F_TO_DBL = 0.0;
        const double BOOL_T_TO_DBL = 1.0;

        // ToSource source representations (None/bool -> str)
        const string NONE_TO_STR = "None";
        const string BOOL_F_TO_STR = "False";
        const string BOOL_T_TO_STR = "True";

        // ToSource float formatting (append ".0" when ToSource output has no decimal point or pow)
        const string DBL_LONG_FRACTION_SUFFIX = ".0";
        const char DBL_POINT_CHAR = '.';
        const char DBL_POW_LOWER_CHAR = 'e';
        const char DBL_POW_UPPER_CHAR = 'E';

        // String.IndexOf "not found" sentinel (used by ToSource float formatting check)
        const int CHAR_NOT_FOUND_INX = -1;

        const int OBJ_FIELD_OFFSET = 0;
        const int LONG_FIELD_OFFSET = 8;
        const int DBL_FIELD_OFFSET = 8;
        const int TAG_FIELD_OFFSET = 16;

        #endregion

    }
}
