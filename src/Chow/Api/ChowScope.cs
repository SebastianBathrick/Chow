using Chow.SourceData;

namespace Chow
{
    public class ChowScope : IChowObject
    {
        public int Length => WrappedObject.Length;

        internal ChowObject WrappedObject
        {
            get;
        }

        public ChowObject this[ChowObject key]
        {
            get => WrappedObject[key];
            set => WrappedObject[key] = value;
        }

        public ChowScope()
        {
            WrappedObject = (ChowObject)ChowObjectFactory.CreateScope();
        }

        internal ChowScope(ChowObject wrappedObject)
        {
            WrappedObject = wrappedObject;
        }

        public static implicit operator ChowObject(ChowScope scope)
        {
            return scope.WrappedObject;
        }

        public static implicit operator ChowScope(ChowObject obj)
        {
            return new ChowScope(obj);
        }

        public override string ToString()
        {
            return WrappedObject.ToString();
        }
    }
}
