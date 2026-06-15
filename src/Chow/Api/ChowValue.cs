using System.Collections.Generic;
using System;
using Chow.SourceData;
using Chow.Utility;

namespace Chow
{
    public sealed class ChowValue
    {
        #region Properties
        
        public static ChowValue None { get; }  = new ChowValue(SourceValue.None);
        
        internal SourceValue SourceValue { get; }

        ISourceObject SourceObject  => _srcObj ?? (_srcObj = SourceValue.ToISourceObject());

        public int Length => SourceObject.Length;
        
        public ChowValue this[ChowValue key]
        {
            get => new ChowValue(SourceObject.GetItem(key.SourceValue));
            set => SourceObject.SetItem(key.SourceValue, value.SourceValue);
        }

        #endregion
        
        ISourceObject _srcObj = null;


        internal ChowValue(SourceValue srcVal)
        {
            SourceValue = srcVal;
        }
        
        public T As<T>()
        {
            return (T)SourceValue.ToObject();
        }

        #region Create Methods

        public static ChowValue CreateDictionary()
        {
            var srcObj = SourceObjectFactory.CreateNewObject(DataType.Dict);
            return new ChowValue(new SourceValue(srcObj));
        }
        
        public static ChowValue CreateList()
        {
            var srcObj = SourceObjectFactory.CreateNewObject(DataType.List);
            return new ChowValue(new SourceValue(srcObj));
        }
        
        #endregion
        
        #region Data List Methods

        public void Add(ChowValue value)
        {
            SourceObject.AppendItem(value.SourceValue);
        }
        
        public void Remove(ChowValue key)
        {
            SourceObject.DeleteItem(key.SourceValue);
        }

        #endregion
        
        #region Call Method Methods
        
        public ChowValue Call()
        {
            return new ChowValue(SourceObject.Call());
        }

        public ChowValue Call(object arg)
        {
            return new ChowValue(SourceObject.Call(new SourceValue(arg)));
        }
        
        public ChowValue Call(object arg1, object arg2, params object[] args)
        {
            if (args == null || args.Length == 0)
            {
                return new ChowValue( 
                    SourceObject.Call(new SourceValue(arg1), new SourceValue(arg2)));
            }
            
            var srcValArgs = new SourceValue[args.Length];

            for (var i = 0; i < args.Length; i++)
            {
                srcValArgs[i] = new SourceValue(args[i]);
            }

            return new ChowValue( 
                SourceObject.Call(new SourceValue(arg1), new SourceValue(arg2), srcValArgs));
        }
        
        #endregion
        
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

            if (l is null || r is null) 
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

        #region Equality Methods
        
        public override bool Equals(object obj)
        {
            return obj is ChowValue other && SourceValue.Equals(other.SourceValue);
        }

        public override int GetHashCode()
        {
            return SourceValue.GetHashCode();
        }

        #endregion

        public override string ToString()
        {
            return SourceValue;
        }
    }
}
