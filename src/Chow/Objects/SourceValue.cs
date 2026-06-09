using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Chow.Exceptions;
using Chow.Objects.Conversion;
using Chow.Utility;
using Chow.VM;
namespace Chow.Objects
{
    /// <summary>
    /// Represents an immutable Chow value of varying Chow data types, with the main types being:
    /// <b>int, float, str, bool, None, list, dict, and range</b>.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct SourceValue
    {
        // TODO: Major refactor going on currently
        public static readonly SourceValue None = new SourceValue(DataType.None);
        
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
        
        /// <summary>Represents the SourceValue equivalent to null/nil/none values.</summary>
        [FieldOffset(OBJ_FIELD_OFFSET)] readonly object _obj;
        [FieldOffset(LONG_FIELD_OFFSET)] readonly long _long;
        [FieldOffset(DBL_FIELD_OFFSET)] readonly double _dbl;
        [FieldOffset(TAG_FIELD_OFFSET)] readonly DataType _dataType;
        
        bool BoolValue => _long == BOOL_T_TO_LONG;

        internal DataType DataType => _dataType;

        #region Constructors

        SourceValue(
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

        internal SourceValue(long value) : this(DataType.Long, longVal: value) {}

        internal SourceValue(double val) : this(DataType.Double, doubleVal: val) {}

        internal SourceValue(bool value) : this(DataType.Bool, value) {}

        internal SourceValue(string value) : this(DataType.Str, objVal: value) {}

        internal SourceValue(SourceList list) : this(DataType.List, objVal: list) {}

        internal SourceValue(SourceDictionary dictionary) : this(DataType.Dict, objVal: dictionary) {}

        internal SourceValue(SourceRange range) : this(DataType.Range, objVal: range) {}

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
                case SourceList listValue:
                    this = new SourceValue(listValue);
                    break;
                case SourceDictionary dictValue:
                    this = new SourceValue(dictValue);
                    break;
                case SourceRange rangeValue:
                    this = new SourceValue(rangeValue);
                    break;
                case SourceValue chowValue:
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
                        // TODO: BinaryAdd error checking for overflow scenarios
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
        
        #region Arithmetic & Logic Operations
        // WARNING: THESE METHODS WILL BE REMOVED IN FUTURE REFACTOR!
        // DO NOT USE ANY OF THESE METHODS OUTSIDE OF THE Processor CLASS

        // Instance methods to avoid passing two ChowValues as parameters. Each returns a new SourceValue
        // (the struct is readonly, so no risk of accidentally mutating this instance's internal state).
        // Promotion rules come from DataTypeConversionMap (the single source of truth). Carve-outs for
        // container/string ops (list+list, list*int, str+str, str*int, dict|dict) are dispatched when
        // the map reports ConversionCase.Nothing.

        internal SourceValue CreateUnion(SourceValue rightOperand)
        {
            if (LookupBinary(ExpressionOperator.BinaryOr, rightOperand) == ConversionCase.Nothing
                && _dataType == DataType.Dict && rightOperand._dataType == DataType.Dict)
            {
                return new SourceValue(SourceDictionary.Merge(AsType<SourceDictionary>(), rightOperand.AsType<SourceDictionary>()));
            }

            throw UnsupportedBinary(ExpressionOperator.BinaryOr, rightOperand);
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

        ConversionCase LookupBinary(ExpressionOperator @operator, SourceValue right)
        {
            return DataTypeConversionMap.GetLeftRightConversionCase(@operator, _dataType, right._dataType);
        }
        
        DataTypeException UnsupportedBinary(ExpressionOperator @operator, SourceValue right)
        {
            return new DataTypeException($"unsupported operand type(s) for {@operator}: '{_dataType}' and '{right._dataType}'");
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
