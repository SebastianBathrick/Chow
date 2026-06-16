using Chow.SourceData;

namespace Chow
{
    public class ChowList : IChowValue
    {
        // Temporary solution
        readonly ChowValue _wrappedObject;

        public int Length => _wrappedObject.Length;

        public ChowValue this[int index]
        {
            get => _wrappedObject[index];
            set => _wrappedObject[index] = value;
        }

        public ChowList()
        {
            _wrappedObject = (ChowValue)ChowValueFactory.CreateList();
        }

        internal ChowList(ChowValue wrappedObject)
        {
            _wrappedObject = wrappedObject;
        }

        public void Append(ChowValue value)
        {
            _wrappedObject.Call(SourceObjectConsts.ListAppendMethodName, value);
        }

        public void Insert(ChowValue index, ChowValue value)
        {
            _wrappedObject.Call(SourceObjectConsts.ListInsertMethodName, index, value);
        }

        public ChowValue Pop(ChowValue index)
        {
            return _wrappedObject.Call(SourceObjectConsts.ListPopMethodName, index);
        }

        public void Remove(ChowValue index)
        {
            _wrappedObject.Call(SourceObjectConsts.ListRemoveMethodName, index);
        }

        public ChowValue Reverse()
        {
            return _wrappedObject.Call(SourceObjectConsts.ListReverseMethodName);
        }

        public void Clear()
        {
            _wrappedObject.Call(SourceObjectConsts.ListClearMethodName);
        }

        public static implicit operator ChowValue(ChowList value)
        {
            return value._wrappedObject;
        }

        public static implicit operator ChowList(ChowValue value)
        {
            return new ChowList(value);
        }

        public override string ToString()
        {
            return _wrappedObject.ToString();
        }
    }
}
