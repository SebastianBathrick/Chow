using System;
using System.Collections.Generic;
using System.Globalization;
using Chow.Interpreter.State.Values;

namespace Chow.Interpreter
{
    public readonly struct ChowValue
    {
        #region Fields

        static readonly Dictionary<Type, DataType> _dataTypeMap = new Dictionary<Type, DataType>()
        {
            { null, DataType.None },
            { typeof(bool), DataType.Bool },
            { typeof(long), DataType.Int },
            { typeof(double), DataType.Float },
            { typeof(string), DataType.Str },
            { typeof(InternalDict), DataType.List },
            { typeof(InternalRange), DataType.Range },
            { typeof(InternalList), DataType.List },
        };
        
        public static readonly ChowValue None = new ChowValue(DataType.None);

        readonly DataType _dataType;
        readonly bool _boolValue;
        readonly object _objectValue;
        
        // In Chow integers are 64 bits instead of 32 bits like C# (hence the "int" and "64" in a field of type long)
        readonly long _int64Value;
        
        // Naming convention is for a similar reason to _int64Value
        readonly double _float64Value;

        #endregion

        #region Properties
        
        bool IsNullableType => 
            _dataType == DataType.Object ||  _dataType == DataType.List ||   _dataType == DataType.Dict ||   _dataType == DataType.Range;

        #endregion
        
        ChowValue(
            DataType dataType = DataType.None, 
            bool boolValue = DEFAULT_BOOL_VALUE, 
            object objectValue = DEFAULT_OBJECT_VALUE, 
            long int64Value = DEFAULT_INT64_VALUE, 
            double float64Value = DEFAULT_FLOAT64_VALUE)
        {
            _dataType = dataType;
            _boolValue = boolValue;
            _objectValue = objectValue;
            _int64Value = int64Value;
            _float64Value = float64Value;

            if (IsNullableType && _objectValue == null)
            {
                throw new ArgumentNullException(nameof(objectValue));
            }
        }

        public TDataType AsType<TDataType>()
        {
            if (!_dataTypeMap.TryGetValue(typeof(TDataType), out var targetDataType))
            {
                if (_objectValue is TDataType typedObject)
                {
                    return typedObject;
                }

                throw new InvalidOperationException($"Cannot convert {_dataType} to {typeof(TDataType)}");
            }

            switch (targetDataType)
            {
                case DataType.Bool:
                {
                    return (TDataType)(object)ToBool();
                }
                case DataType.Int:
                {
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

            throw new InvalidOperationException($"Cannot convert {_dataType} to {typeof(TDataType)}");
        }

        public bool IsOfType<TDataType>()
        {
            // If it is not a type defined by the DataType enum
            if (!_dataTypeMap.ContainsKey(typeof(TDataType)))
            {
                return _dataType == DataType.Object && _objectValue is TDataType;
            }
            
            // The map includes values representing data types that are from the Chow.Interpreter namespace
            var chowDataType = _dataTypeMap[typeof(TDataType)];
            return _dataType == chowDataType;
        }

        #region Conversion Methods
        // These methods can be indirectly accessed via the AsType<T>() method. Even the VirtualMachine does not need direct access to these.
        
        bool ToBool()
        {
            switch (_dataType)
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

        long ToInt64()
        {
            switch (_dataType)
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

        double ToFloat64()
        {
            switch (_dataType)
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

        string ToStr()
        {
            switch (_dataType)
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
                        throw new InvalidOperationException($"{nameof(ChowValue)} object with type {_dataType} null");
                    }

                    return _objectValue.ToString();
                }
            }

            throw new InvalidOperationException();
        }

        #endregion

        #region Conversion Helpers

        private bool StrToBool()
        {
            var strValue = _objectValue as string;

            if (strValue != null)
            {
                return strValue.Length != STR_LENGTH_REP_BOOL_FALSE;
            }

            throw new InvalidOperationException("Expected string value for boolean comparison");
        }

        private bool ListToBool()
        {
            var listValue = _objectValue as InternalList;

            if (listValue != null)
            {
                return listValue.Count != LIST_COUNT_REP_BOOL_FALSE;
            }

            throw new InvalidOperationException("Expected list value for boolean comparison");
        }

        private bool DictToBool()
        {
            var dictValue = _objectValue as InternalDict;

            if (dictValue != null)
            {
                return dictValue.Count != DICT_COUNT_REP_BOOL_FALSE;
            }

            throw new InvalidOperationException("Expected dict value for boolean comparison");
        }

        private bool RangeToBool()
        {
            var rangeValue = _objectValue as InternalRange;

            if (rangeValue != null)
            {
                return rangeValue.Count != RANGE_COUNT_REP_BOOL_FALSE;
            }

            throw new InvalidOperationException("Expected range value for boolean comparison");
        }

        private long StrToInt64()
        {
            var strValue = _objectValue as string;

            if (strValue == null)
            {
                throw new InvalidOperationException("Expected string value for int64 conversion");
            }

            if (long.TryParse(strValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt64))
            {
                return parsedInt64;
            }

            throw new InvalidOperationException($"Cannot convert string '{strValue}' to int64");
        }

        private double StrToFloat64()
        {
            var strValue = _objectValue as string;

            if (strValue == null)
            {
                throw new InvalidOperationException("Expected string value for float64 conversion");
            }

            if (double.TryParse(strValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedFloat64))
            {
                return parsedFloat64;
            }

            throw new InvalidOperationException($"Cannot convert string '{strValue}' to float64");
        }

        private string FloatToStr()
        {
            var formatted = _float64Value.ToString(CultureInfo.InvariantCulture);

            if (IsFractionalSuffix(formatted))
            {
                formatted += FLOAT64_INTEGER_FRACTIONAL_SUFFIX;
            }

            return formatted;
        }

        private static bool IsFractionalSuffix(string formatted)
        {
            return formatted.IndexOf(FLOAT64_DECIMAL_POINT_CHAR) == CHAR_NOT_FOUND_INDEX
                   && formatted.IndexOf(FLOAT64_EXPONENT_LOWER_CHAR) == CHAR_NOT_FOUND_INDEX
                   && formatted.IndexOf(FLOAT64_EXPONENT_UPPER_CHAR) == CHAR_NOT_FOUND_INDEX;
        }

        private string StrToStr()
        {
            var strValue = _objectValue as string;

            if (strValue != null)
            {
                return strValue;
            }

            throw new InvalidOperationException("Expected string value for string conversion");
        }

        #endregion

        #region Constants

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