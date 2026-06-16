using System.Collections.Generic;
using System;
using Chow.SourceData;
using Chow.Utility;
using Chow.VM;

namespace Chow
{
    public sealed class ChowValue : IChowValue
    {
        #region Properties

        public static ChowValue None { get; } 
            = (ChowValue)ApiConverter.Convert(SourceData.SourceValue.None);
        
        internal SourceValue SourceValue { get; }

        ISourceObject SourceObject  => _srcObj ?? (_srcObj = SourceValue.ToISourceObject());

        public int Length => SourceObject.Length;
        
        public ChowValue this[ChowValue key]
        {
            get => new ChowValue(SourceObject.GetItem(key.SourceValue));
            set => SourceObject.SetItem(key.SourceValue, value.SourceValue);
        }

        // This is primarily for testing. Avoid using internally if possible
        internal ChowValue(SourceValue srcVal)
        {
            SourceValue = srcVal;
        }

        #endregion
        
        ISourceObject _srcObj = null;

        internal ChowValue(ref SourceValue srcVal)
        {
            SourceValue = srcVal;
        }
        
        public T As<T>()
        {
            return (T)SourceValue.ToObject();
        }

        #region Factory Methods
        
        public static ChowValue CreateList()
        {
            // Cast, because IChowValue has no public API or implicit operators for the client
            return (ChowValue)ChowValueFactory.CreateList();
        }

        public static ChowValue CreateDictionary()
        {
            // Cast, because IChowValue has no public API or implicit operators for the client
            return (ChowValue)ChowValueFactory.CreateDictionary();
        }
        
        #endregion

        #region Attribute Methods

        public ChowValue GetAttribute(ChowValue name)
        {
            var attr = SourceObject.GetAttribute(name.SourceValue);
            var chowVal = new ChowValue(attr);
            return chowVal;
        }

        #endregion
        
        #region Call Self Method Methods
        
        public ChowValue Call(string methodName, params ChowValue[] args)
        {
            var methodAttr = SourceObject.GetAttribute(methodName);
            
            return new ChowValue(ChowEngine.Call(methodAttr, ApiConverter.Convert(args)));
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

        public static implicit operator ChowValue(Func<object> value)
        {
            Func<SourceValue[], SourceValue> wrapper = _ =>
            {
                var result = value();
                return result is null ? SourceValue.None : new SourceValue(result);
            };
            return new ChowValue(new SourceValue(wrapper));
        }

        public static implicit operator ChowValue(Action<object> value)
        {
            Func<SourceValue[], SourceValue> wrapper = args =>
            {
                value(args != null && args.Length > 0 ? args[0].ToObject() : null);
                return SourceValue.None;
            };
            return new ChowValue(new SourceValue(wrapper));
        }

        public static implicit operator ChowValue(Action<object[]> value)
        {
            Func<SourceValue[], SourceValue> wrapper = args =>
            {
                value(SourceValue.ToObjects(args ?? Array.Empty<SourceValue>()));
                return SourceValue.None;
            };
            return new ChowValue(new SourceValue(wrapper));
        }

        public static implicit operator ChowValue(Func<object[], object> value)
        {
            Func<SourceValue[], SourceValue> wrapper = args =>
            {
                var result = value(SourceValue.ToObjects(args ?? Array.Empty<SourceValue>()));
                return result is null ? SourceValue.None : new SourceValue(result);
            };
            return new ChowValue(new SourceValue(wrapper));
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
