using Chow.Interpreter.State.Values;
namespace Chow.Interpreter.Values
{
    public class ChowList : ChowValue
    {
        public ChowList()
        {
            Internal = new InternalList();
        }

        public ChowList(ChowList source)
        {
            Internal = InternalList.Concat(source.Internal, new InternalList());
        }

        internal ChowList(InternalList wrapped)
        {
            Internal = wrapped;
        }
        internal InternalList Internal { get; }

        public int Count => Internal.Count;

        public ChowValue this[int index] => ApiConverter.ToChowValue(Internal[index]);

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
