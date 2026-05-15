using Chow.Interpreter.State.Values;
namespace Chow.Interpreter.Values
{
    public class ChowDict : ChowValue
    {
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
        internal InternalDict Internal { get; }

        public int Count => Internal.Count;

        public ChowValue this[ChowValue key]
            => ApiConverter.ToChowValue(Internal[ApiConverter.ToTaggedUnion(key)]);

        public override TDataType AsType<TDataType>()
        {
            if (typeof(TDataType) == typeof(bool))
            {
                return (TDataType)(object)(Internal.Count != 0);
            }

            throw new InvalidCastException(GetType(), typeof(TDataType), this);
        }

        public override bool IsType<TDataType>()
        {
            return false;
        }

        public override string ToString()
        {
            return Internal.ToString();
        }
    }
}
