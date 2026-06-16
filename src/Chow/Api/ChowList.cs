using Chow.SourceData;

namespace Chow
{
    public class ChowList : IChowObject
    {
        // Temporary solution

        internal ChowObject WrappedObject
        {
            get;
        }

        public int Length => WrappedObject.Length;

        public ChowObject this[int index]
        {
            get => WrappedObject[index];
            set => WrappedObject[index] = value;
        }

        public ChowList()
        {
            WrappedObject = (ChowObject)ChowObjectFactory.CreateList();
        }

        internal ChowList(ChowObject wrappedObject)
        {
            WrappedObject = wrappedObject;
        }

        public void Append(ChowObject @object)
        {
            WrappedObject.Call(SourceObjectConsts.ListAppendMethodName, @object);
        }

        public void Insert(ChowObject index, ChowObject @object)
        {
            WrappedObject.Call(SourceObjectConsts.ListInsertMethodName, index, @object);
        }

        public ChowObject Pop(ChowObject index)
        {
            return WrappedObject.Call(SourceObjectConsts.ListPopMethodName, index);
        }

        public void Remove(ChowObject index)
        {
            WrappedObject.Call(SourceObjectConsts.ListRemoveMethodName, index);
        }

        public ChowObject Reverse()
        {
            return WrappedObject.Call(SourceObjectConsts.ListReverseMethodName);
        }

        public void Clear()
        {
            WrappedObject.Call(SourceObjectConsts.ListClearMethodName);
        }

        public static implicit operator ChowObject(ChowList value)
        {
            return value.WrappedObject;
        }

        public static implicit operator ChowList(ChowObject @object)
        {
            return new ChowList(@object);
        }

        public override string ToString()
        {
            return WrappedObject.ToString();
        }
    }
}
