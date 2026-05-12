using Chow.Interpreter.State.Values;

namespace Chow.Interpreter.Values
{
    public class ChowList : ChowValue
    {
        internal InternalList Internal { get; }

        public int Count => Internal.Count;

        public ChowValue this[int index] => ChowValueConverter.ToChowValue(Internal[index]);

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
