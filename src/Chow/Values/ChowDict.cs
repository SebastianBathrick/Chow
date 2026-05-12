using Chow.Interpreter.State.Values;

namespace Chow.Interpreter.Values
{
    public class ChowDict : ChowValue
    {
        internal InternalDict Internal { get; }

        public int Count => Internal.Count;

        public ChowValue this[ChowValue key]
            => ChowValueConverter.ToChowValue(Internal[ChowValueConverter.ToTaggedUnion(key)]);

        public ChowDict()
        {
            Internal = new InternalDict();
        }

        public ChowDict(ChowDict source)
        {
            Internal = InternalDict.Merge(source.Internal, new InternalDict());
        }

        internal ChowDict(InternalDict wrapped)
        {
            Internal = wrapped;
        }

        public override TDataType As<TDataType>()
        {
            if (typeof(TDataType) == typeof(bool))
            {
                return (TDataType)(object)(Internal.Count != 0);
            }

            throw new InvalidCastException(GetType(), typeof(TDataType), this);
        }

        public override bool Is<TDataType>()
        {
            return false;
        }

        public override string ToString() => Internal.ToString();
    }
}
