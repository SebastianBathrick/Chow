using Chow.SourceData;

namespace Chow
{
    public class ChowDictionary : IChowValue
    {
        // Temporary solution
        readonly ChowValue _wrappedObject;

        public int Length => _wrappedObject.Length;

        public ChowValue this[ChowValue key]
        {
            get => _wrappedObject[key];
            set => _wrappedObject[key] = value;
        }

        public ChowDictionary()
        {
            _wrappedObject = (ChowValue)ChowValueFactory.CreateDictionary();
        }

        internal ChowDictionary(ChowValue wrappedObject)
        {
            _wrappedObject = wrappedObject;
        }

        public ChowValue Get(ChowValue key)
        {
            return _wrappedObject.Call(SourceObjectConsts.DictionaryGetMethodName, key);
        }

        public ChowValue Get(ChowValue key, ChowValue defaultValue)
        {
            return _wrappedObject.Call(SourceObjectConsts.DictionaryGetMethodName, key, defaultValue);
        }

        public ChowValue Pop(ChowValue key)
        {
            return _wrappedObject.Call(SourceObjectConsts.DictionaryPopMethodName, key);
        }

        public ChowValue Pop(ChowValue key, ChowValue defaultValue)
        {
            return _wrappedObject.Call(SourceObjectConsts.DictionaryPopMethodName, key, defaultValue);
        }

        public void Update(ChowValue other)
        {
            _wrappedObject.Call(SourceObjectConsts.DictionaryUpdateMethodName, other);
        }

        public ChowValue SetDefault(ChowValue key)
        {
            return _wrappedObject.Call(SourceObjectConsts.DictionarySetMethodName, key);
        }

        public ChowValue SetDefault(ChowValue key, ChowValue defaultValue)
        {
            return _wrappedObject.Call(SourceObjectConsts.DictionarySetMethodName, key, defaultValue);
        }

        public void Clear()
        {
            _wrappedObject.Call(SourceObjectConsts.DictionaryClearMethodName);
        }

        public static implicit operator ChowValue(ChowDictionary value)
        {
            return value._wrappedObject;
        }

        public static implicit operator ChowDictionary(ChowValue value)
        {
            return new ChowDictionary(value);
        }

        public override string ToString()
        {
            return _wrappedObject.ToString();
        }
    }
}
