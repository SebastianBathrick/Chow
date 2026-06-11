using System.Collections.Generic;
using System;
using Chow.SourceData;
using Chow.Utility;

namespace Chow
{
    public class ChowValue
    {
        public static ChowValue None { get; }  = new ChowValue(SourceValue.None);
        
        internal SourceValue SourceValue { get; }
        
        internal ChowValue(SourceValue srcVal)
        {
            SourceValue = srcVal;
        }
        
        public T As<T>()
        {
            return (T)SourceValue.ToObject();
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
            return new ChowValue(value);
        }

        public static implicit operator long(ChowValue value)
        {
            return value.SourceValue.ToLong();
        }

        public static implicit operator ChowValue(double value)
        {
            return new ChowValue(value);
        }

        public static implicit operator double(ChowValue value)
        {
            return value.SourceValue.ToDouble();
        }

        public static implicit operator ChowValue(string value)
        {
            return new ChowValue(value);
        }

        public static implicit operator string(ChowValue value)
        {
            return value.SourceValue.ToString();
        }

        public static bool operator ==(ChowValue l, ChowValue r)
        {
            if (ReferenceEquals(l, r)) 
            {
                return true;
            }

            if ( l is null || r is null) 
            {
                return false;
            }

            return l.SourceValue.Equals(r.SourceValue);
        }

        public static bool operator !=(ChowValue l, ChowValue r)
        {
            return !(l == r);
        }

        public static bool operator ==(ChowValue l, bool r)
        {
            return l?.SourceValue.ToBool() == r;
        }

        public static bool operator !=(ChowValue l, bool r)
        {
            return !(l == r);
        }

        public static bool operator ==(ChowValue l, long r)
        {
            return l?.SourceValue.ToLong() == r;
        }

        public static bool operator !=(ChowValue l, long r)
        {
            return !(l == r);
        }

        public static bool operator ==(ChowValue l, double r)
        {
            return  !(l is null) && l.SourceValue.ToDouble().Equals(r);
        }

        public static bool operator !=(ChowValue l, double r)
        {
            return !(l == r);
        }
        
        #endregion
        
    }
}
