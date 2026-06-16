using Chow.SourceData;

namespace Chow
{
    public class ChowDictionary : IChowObject
    {
        // Temporary solution

        internal ChowObject WrappedObject
        {
            get;
        }

        public int Length => WrappedObject.Length;

        public ChowObject this[ChowObject key]
        {
            get => WrappedObject[key];
            set => WrappedObject[key] = value;
        }

        public ChowDictionary()
        {
            WrappedObject = (ChowObject)ChowObjectFactory.CreateDictionary();
        }

        internal ChowDictionary(ChowObject wrappedObject)
        {
            WrappedObject = wrappedObject;
        }

        public ChowObject Get(ChowObject key)
        {
            return WrappedObject.Call(SourceObjectConsts.DictionaryGetMethodName, key);
        }

        public ChowObject Get(ChowObject key, ChowObject defaultObject)
        {
            return WrappedObject.Call(SourceObjectConsts.DictionaryGetMethodName, key, defaultObject);
        }

        public ChowObject Pop(ChowObject key)
        {
            return WrappedObject.Call(SourceObjectConsts.DictionaryPopMethodName, key);
        }

        public ChowObject Pop(ChowObject key, ChowObject defaultObject)
        {
            return WrappedObject.Call(SourceObjectConsts.DictionaryPopMethodName, key, defaultObject);
        }

        public void Update(ChowObject other)
        {
            WrappedObject.Call(SourceObjectConsts.DictionaryUpdateMethodName, other);
        }

        public ChowObject SetDefault(ChowObject key)
        {
            return WrappedObject.Call(SourceObjectConsts.DictionarySetMethodName, key);
        }

        public ChowObject SetDefault(ChowObject key, ChowObject defaultObject)
        {
            return WrappedObject.Call(SourceObjectConsts.DictionarySetMethodName, key, defaultObject);
        }

        public void Clear()
        {
            WrappedObject.Call(SourceObjectConsts.DictionaryClearMethodName);
        }

        public static implicit operator ChowObject(ChowDictionary value)
        {
            return value.WrappedObject;
        }

        public static implicit operator ChowDictionary(ChowObject @object)
        {
            return new ChowDictionary(@object);
        }

        public override string ToString()
        {
            return WrappedObject.ToString();
        }
    }
}
