using Chow.Interpreter.Values.Internal;

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

        public override DataType As<DataType>()
        {
            if (typeof(DataType) == typeof(bool))
            {
                return (DataType)(object)(Internal.Count != 0);
            }

            throw new InvalidCastException(GetType(), typeof(DataType), this);
        }

        public override bool Is<DataType>()
        {
            return false;
        }

        public override string ToString() => Internal.ToString();
    }
}
