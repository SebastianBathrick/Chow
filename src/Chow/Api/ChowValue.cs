using System.Collections.Generic;
using Chow.Objects;
using System;
using Chow.Utility;

namespace Chow
{
    public class ChowValue
    {
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

        internal SourceValue SourceValue { get; }
        
        internal ChowValue(SourceValue srcVal)
        {
            SourceValue = srcVal;
        }
        
        public T AsType<T>()
        {
            return SourceValue.AsType<T>();
        }

        public override bool Equals(object obj)
        {
            return obj is ChowValue other && SourceValue.Equals(other.SourceValue);
        }

        public override int GetHashCode()
        {
            return SourceValue.GetHashCode();
        }

        #region Implicit Operators
        
        public static implicit operator ChowValue(bool value)
        {
            return new ChowValue(new SourceValue(value));
        }

        public static implicit operator bool(ChowValue value)
        {
            return value.SourceValue.ToBool();
        }

        public static implicit operator ChowValue(long value)
        {
            return new ChowValue(new SourceValue(value));
        }

        public static implicit operator long(ChowValue value)
        {
            return value.SourceValue.ToLong();
        }

        public static implicit operator ChowValue(double value)
        {
            return new ChowValue(new SourceValue(value));
        }

        public static implicit operator double(ChowValue value)
        {
            return value.SourceValue.ToDouble();
        }

        public static implicit operator ChowValue(string value)
        {
            return new ChowValue(new SourceValue(value));
        }

        public static implicit operator string(ChowValue value)
        {
            return value.SourceValue.ToString();
        }

        public static bool operator ==(ChowValue left, ChowValue right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.SourceValue.Equals(right.SourceValue);
        }

        public static bool operator !=(ChowValue left, ChowValue right)
        {
            return !(left == right);
        }

        public static bool operator ==(ChowValue left, bool right)
        {
            return left?.SourceValue.ToBool() == right;
        }

        public static bool operator !=(ChowValue left, bool right)
        {
            return !(left == right);
        }

        public static bool operator ==(ChowValue left, long right)
        {
            return left?.SourceValue.ToLong() == right;
        }

        public static bool operator !=(ChowValue left, long right)
        {
            return !(left == right);
        }

        public static bool operator ==(ChowValue left, double right)
        {
            return  !(left is null) && left.SourceValue.ToDouble().Equals(right);
        }

        public static bool operator !=(ChowValue left, double right)
        {
            return !(left == right);
        }

        public static bool operator ==(ChowValue left, string right)
        {
            return left?.SourceValue.ToString() == right;
        }

        public static bool operator !=(ChowValue left, string right)
        {
            return !(left == right);
        }
        
        #endregion
        
    }
}
