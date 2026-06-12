using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using Chow.Utility;
using Chow.VM;
namespace Chow.SourceData
{
    /// <summary>
    /// Represents an immutable Chow value of varying Chow data types, with the main types being:
    /// <b>int, float, str, bool, None, list, dict, and range</b>.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    readonly struct SourceValue : IEquatable<SourceValue>
    {
        // TODO: Major refactor going on currently
        public static readonly SourceValue None = new SourceValue(DataType.None);
        
        /// <summary>Represents the SourceValue equivalent to null/nil/none values.</summary>
        [FieldOffset(ObjectFieldOffset)] readonly object _obj;
        [FieldOffset(LongFieldOffset)] readonly long _long;
        [FieldOffset(DoubleFieldOffset)] readonly double _dbl;
        [FieldOffset(TagFieldOffset)] readonly DataType _dataType;
        
        bool BoolValue => _long == BoolTrueToLong;

        internal DataType DataType => _dataType;

        #region Constructors

        SourceValue(
            DataType dataType = DataType.None,
            bool boolValue = NotBoolInitialValue,
            object objVal = NotObjectInitialValue,
            long longVal = NotLongInitialValue,
            double doubleVal = NotDoubleInitialValue)
        {
            _dataType = dataType;
            _obj = objVal;

            // _long and _dbl share the same FieldOffset (explicit-layout union). The compiler still
            // requires both to be definitely assigned, so every branch sets the live field to its
            // value and the dead field to its default — each field is written exactly once.
            switch (dataType)
            {
                case DataType.Bool:
                    _dbl = NotDoubleInitialValue;
                    _long = boolValue ? BoolTrueToLong : BoolFalseToLong;
                    break;
                case DataType.Double:
                    _long = NotLongInitialValue;
                    _dbl = doubleVal;
                    break;
                case DataType.Object:
                case DataType.List:
                case DataType.Dict:
                case DataType.Range:
                case DataType.Function:
                case DataType.Slice:
                case DataType.Str:
                case DataType.None:
                case DataType.Long:
                    _dbl = NotDoubleInitialValue;
                    _long = longVal;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
            }
        }

        internal SourceValue(long value) : this(DataType.Long, longVal: value) {}

        internal SourceValue(double val) : this(DataType.Double, doubleVal: val) {}

        internal SourceValue(bool value) : this(DataType.Bool, value) {}

        internal SourceValue(string value) : this(DataType.Str, objVal: value) {}

        /// <summary>The value's tag comes from the object itself, so every ISourceObject kind shares this path.</summary>
        internal SourceValue(ISourceObject srcObj) : this(srcObj.Type, objVal: srcObj) {}

        /// <summary>
        /// Resolves and converts the value of <paramref name="obj"/> and initializes instance with
        /// the converted value (if a dataType is defined for that type).
        /// </summary>
        internal SourceValue(object obj)
        {
            switch (obj)
            {
                case null:
                // TODO: Look into changing this to SourceValue.None.
                    throw new ArgumentNullException(nameof(obj));
                case string strValue:
                    this = new SourceValue(strValue);
                    break;
                case long longValue:
                    this = new SourceValue(longValue);
                    break;
                case int intValue:
                    this = new SourceValue(intValue);
                    break;
                case double doubleValue:
                    this = new SourceValue(doubleValue);
                    break;
                case bool boolValue:
                    this = new SourceValue(boolValue);
                    break;
                case ISourceObject srcObj:
                    this = new SourceValue(srcObj);
                    break;
                case SourceValue chowValue:
                    // **IMPORTANT**: CHOW VALUES ARE NEVER DIRECTLY WRAPPED IN OTHER CHOW VALUE INSTANCES
                    this = chowValue;
                    break;
                default:
                    _dataType = DataType.Object;
                    _obj = obj;
                    _long = NotLongInitialValue;
                    _dbl = NotDoubleInitialValue;
                    break;
            }
        }

        #endregion

        #region Interop

        internal SourceValue InvokeHostDelegate(SourceValue[] args)
        {
            if (_dataType != DataType.Object)
            {
                throw new DataTypeException($"'{_dataType}' object is not callable");
            }

            if (_obj is Func<SourceValue[], SourceValue> methodDelegate)
            {
                return methodDelegate(args ?? Array.Empty<SourceValue>());
            }

            throw new InvalidOperationException($"Object of type '{_obj.GetType().Name}' is not callable");
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
                    return NoneToBool;

                case DataType.Bool:
                    return BoolValue;

                case DataType.Object:
                    return ObjectToBool;

                case DataType.Long:
                    return _long != LongToBoolFalse;

                case DataType.Double:
                    return Math.Abs(_dbl - DoubleToBoolFalse) > TOLERANCE;

                case DataType.Str:
                    return StringToBool();

                case DataType.List:
                case DataType.Dict:
                case DataType.Range:
                case DataType.Function:
                case DataType.Slice:
                    return ((ISourceObject)_obj).Truthiness;
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
                    return BoolValue ? BoolTrueToLong : BoolFalseToLong;
                    
                case DataType.Long:
                    return _long;
                    
                case DataType.Double:
                    return (long)_dbl;
                    
                case DataType.Str:
                    return StringToLong();
                    
                default:
                    throw new DataTypeException(GetConversionErrorMessage(_dataType, DataType.Long));
            }

        }

        internal double ToDouble()
        {
            switch (_dataType)
            {
                case DataType.Bool:
                    return BoolValue ? BoolTrueToDouble : BoolFalseToDouble;

                case DataType.Long:
                    return _long;

                case DataType.Double:
                    return _dbl;

                case DataType.Str:
                    return StringToDouble();
                    
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

        internal ISourceObject ToISourceObject()
        {
            return (ISourceObject)_obj;
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
                    return FloatToString();

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

        bool StringToBool()
        {
            if (_obj is string strValue)
            {
                return strValue.Length != StringLengthToBool;
            }

            throw new InvalidOperationException("Expected string value for boolean comparison");
        }
        long StringToLong()
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

        double StringToDouble()
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

        string FloatToString()
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

        #region Constants

        // GetHashCode mixing multiplier (standard small prime for combining hashes)
        const int HashCombinePrime = 397;

        // Constructor defaults
        const bool NotBoolInitialValue = false;
        const object NotObjectInitialValue = null;
        const long NotLongInitialValue = 0L;
        const double NotDoubleInitialValue = 0.0;

        // ToBool source representations (numeric "false" values)
        const long LongToBoolFalse = 0L;
        const double DoubleToBoolFalse = 0.0;

        // ToBool source representations (container/string "false" lengths, plus None/Object fixed reps)
        const int StringLengthToBool = 0;
        const bool NoneToBool = false;
        const bool ObjectToBool = true;

        // ToLong source representations (bool -> long)
        const long BoolFalseToLong = 0L;
        const long BoolTrueToLong = 1L;

        // ToDouble source representations (bool -> double)
        const double BoolFalseToDouble = 0.0;
        const double BoolTrueToDouble = 1.0;

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

        const int ObjectFieldOffset = 0;
        const int LongFieldOffset = 8;
        const int DoubleFieldOffset = 8;
        const int TagFieldOffset = 16;

        #endregion

        #region Implicit Operators

        public static implicit operator SourceValue(bool value)
        {
            return new SourceValue(value);
        }

        public static implicit operator bool(SourceValue value)
        {
            return value.ToBool();
        }

        public static implicit operator SourceValue(long value)
        {
            return new SourceValue(value);
        }

        public static implicit operator long(SourceValue value)
        {
            return value.ToLong();
        }

        public static implicit operator SourceValue(double value)
        {
            return new SourceValue(value);
        }

        public static implicit operator double(SourceValue value)
        {
            return value.ToDouble();
        }

        public static implicit operator SourceValue(string value)
        {
            return new SourceValue(value);
        }

        public static implicit operator string(SourceValue value)
        {
            return value.ToString();
        }
        
        #endregion

        public static SourceValue Add(SourceValue r, SourceValue l)
        {
            return ArithmeticEvaluator.Add(ref r, ref l);
        }

        public static SourceValue Subtract(SourceValue r, SourceValue l)
        {
            return ArithmeticEvaluator.Subtract(ref r, ref l);
        }

        public static SourceValue Multiply(SourceValue r, SourceValue l)
        {
            return ArithmeticEvaluator.Multiply(ref r, ref l);
        }

        public static SourceValue Divide(SourceValue r, SourceValue l)
        {
            return ArithmeticEvaluator.Divide(ref r, ref l);
        }

        public static SourceValue Mod(SourceValue r, SourceValue l)
        {
            return ArithmeticEvaluator.Mod(ref r, ref l);
        }

        public static SourceValue Floor(SourceValue r, SourceValue l)
        {
            return ArithmeticEvaluator.EvaluateFloorDivision(ref r, ref l);
        }

        public static SourceValue Pow(SourceValue r, SourceValue l)
        {
            return ArithmeticEvaluator.Pow(ref r, ref l);
        }

        public static SourceValue Negate(SourceValue operand)
        {
            return ArithmeticEvaluator.EvaluateNegation(ref operand);
        }

        public static SourceValue IsEqual(SourceValue r, SourceValue l)
        {
            return ComparisonEvaluator.EvaluateEqual(ref r, ref l);
        }

        public static SourceValue IsNotEqual(SourceValue r, SourceValue l)
        {
            return ComparisonEvaluator.EvaluateNotEqual(ref r, ref l);
        }

        public static SourceValue IsLess(SourceValue r, SourceValue l)
        {
            return ComparisonEvaluator.EvaluateLess(ref r, ref l);
        }

        public static SourceValue IsGreater(SourceValue r, SourceValue l)
        {
            return ComparisonEvaluator.EvaluateGreater(ref r, ref l);
        }

        public static SourceValue IsLessOrEqual(SourceValue r, SourceValue l)
        {
            return ComparisonEvaluator.EvaluateLessEqual(ref r, ref l);
        }

        public static SourceValue IsGreaterOrEqual(SourceValue r, SourceValue l)
        {
            return ComparisonEvaluator.EvaluateGreaterEqual(ref r, ref l);
        }

        public static SourceValue And(SourceValue r, SourceValue l)
        {
            return LogicEvaluator.EvaluateAnd(ref r, ref l);
        }

        public static SourceValue Or(SourceValue r, SourceValue l)
        {
            return LogicEvaluator.EvaluateOr(ref r, ref l);
        }

        public static SourceValue Unite(SourceValue r, SourceValue l)
        {
            return LogicEvaluator.EvaluateUnion(ref r, ref l);
        }

        public static SourceValue Not(SourceValue operand)
        {
            return LogicEvaluator.EvaluateNot(ref operand);
        }
        
        public bool Equals(SourceValue other)
        {
            // Cheapest, most-discriminating check first; _long and _dbl overlap in the
            // explicit-layout union, so one bitwise compare covers both numeric fields.
            return _dataType == other._dataType && _long == other._long && Equals(_obj, other._obj);
        }

        public override bool Equals(object obj)
        {
            return obj is SourceValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                // _dbl shares _long's bits in the union, so hashing _long covers both.
                var hashCode = (_obj != null ? _obj.GetHashCode() : 0);
                hashCode = hashCode * HashCombinePrime ^ _long.GetHashCode();
                hashCode = hashCode * HashCombinePrime ^ (int)_dataType;
                return hashCode;
            }
        }
    }
}
