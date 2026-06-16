using System;
using Chow.SourceData;

namespace Chow
{
    public sealed class ChowObject : IChowObject
    {
        #region Properties

        public static ChowObject None { get; }
            = (ChowObject)ApiConverter.Convert(SourceValue.None);

        internal SourceValue SourceValue { get; }

        ISourceObject SourceObject => _srcObj ?? (_srcObj = SourceValue.ToISourceObject());

        public int Length => SourceObject.Length;

        public ChowObject this[ChowObject key]
        {
            get => new ChowObject(SourceObject.GetItem(key.SourceValue));
            set => SourceObject.SetItem(key.SourceValue, value.SourceValue);
        }

        // This is primarily for testing. Avoid using internally if possible
        internal ChowObject(SourceValue srcVal)
        {
            SourceValue = srcVal;
        }

        #endregion

        ISourceObject _srcObj;

        internal ChowObject(ref SourceValue srcVal)
        {
            SourceValue = srcVal;
        }

        public T As<T>()
        {
            return (T)SourceValue.ToObject();
        }

        #region Factory Methods

        public static ChowObject CreateList()
        {
            // Cast, because IChowObject has no public API or implicit operators for the client
            return (ChowObject)ChowObjectFactory.CreateList();
        }

        public static ChowObject CreateDictionary()
        {
            // Cast, because IChowObject has no public API or implicit operators for the client
            return (ChowObject)ChowObjectFactory.CreateDictionary();
        }

        public static ChowObject CreateScope()
        {
            // Cast, because IChowObject has no public API or implicit operators for the client
            return (ChowObject)ChowObjectFactory.CreateScope();
        }

        #endregion

        #region Attribute Methods

        public ChowObject GetAttribute(ChowObject name)
        {
            var attr = SourceObject.GetAttribute(name.SourceValue);
            var chowVal = new ChowObject(attr);
            return chowVal;
        }

        #endregion

        #region Call Self Method Methods

        public ChowObject Call(string methodName, params ChowObject[] args)
        {
            var methodAttr = SourceObject.GetAttribute(methodName);
            var convertedArgs = ApiConverter.ConvertToInterface(args);
            
            return (ChowObject)ChowEngine.Call(ref methodAttr, convertedArgs);
        }

        #endregion

        #region Implicit Operators

        public static implicit operator ChowObject(bool value)
        {
            return new ChowObject(new SourceValue(value));
        }

        public static implicit operator bool(ChowObject @object)
        {
            return @object.SourceValue.ToBool();
        }

        public static implicit operator ChowObject(long value)
        {
            return new ChowObject(value);
        }

        public static implicit operator long(ChowObject @object)
        {
            return @object.SourceValue.ToLong();
        }

        public static implicit operator ChowObject(double value)
        {
            return new ChowObject(value);
        }

        public static implicit operator double(ChowObject @object)
        {
            return @object.SourceValue.ToDouble();
        }

        public static implicit operator ChowObject(string value)
        {
            return new ChowObject(value);
        }

        public static implicit operator string(ChowObject @object)
        {
            return @object.SourceValue.ToString();
        }

        public static implicit operator ChowObject(Func<object> value)
        {
            Func<SourceValue[], SourceValue> wrapper = _ =>
            {
                var result = value();
                return result is null ? SourceValue.None : new SourceValue(result);
            };

            return new ChowObject(new SourceValue(wrapper));
        }

        public static implicit operator ChowObject(Action<object> value)
        {
            Func<SourceValue[], SourceValue> wrapper = args =>
            {
                value(args != null && args.Length > 0 ? args[0].ToObject() : null);
                return SourceValue.None;
            };

            return new ChowObject(new SourceValue(wrapper));
        }

        public static implicit operator ChowObject(Action<object[]> value)
        {
            Func<SourceValue[], SourceValue> wrapper = args =>
            {
                value(SourceValue.ToObjects(args ?? Array.Empty<SourceValue>()));
                return SourceValue.None;
            };

            return new ChowObject(new SourceValue(wrapper));
        }

        public static implicit operator ChowObject(Func<object[], object> value)
        {
            Func<SourceValue[], SourceValue> wrapper = args =>
            {
                var result = value(SourceValue.ToObjects(args ?? Array.Empty<SourceValue>()));
                return result is null ? SourceValue.None : new SourceValue(result);
            };

            return new ChowObject(new SourceValue(wrapper));
        }

        public static bool operator ==(ChowObject l, ChowObject r)
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

        public static bool operator !=(ChowObject l, ChowObject r)
        {
            return !(l == r);
        }

        public static bool operator ==(ChowObject l, bool r)
        {
            return l?.SourceValue.ToBool() == r;
        }

        public static bool operator !=(ChowObject l, bool r)
        {
            return !(l == r);
        }

        public static bool operator ==(ChowObject l, long r)
        {
            return l?.SourceValue.ToLong() == r;
        }

        public static bool operator !=(ChowObject l, long r)
        {
            return !(l == r);
        }

        public static bool operator ==(ChowObject l, double r)
        {
            return !(l is null) && l.SourceValue.ToDouble().Equals(r);
        }

        public static bool operator !=(ChowObject l, double r)
        {
            return !(l == r);
        }

        #endregion

        #region Equality Methods

        public override bool Equals(object obj)
        {
            return obj is ChowObject other && SourceValue.Equals(other.SourceValue);
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
