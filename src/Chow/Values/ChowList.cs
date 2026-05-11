using Chow.Interpreter.Values.Internal;

namespace Chow.Interpreter.Values
{
    public class ChowList : ChowValue
    {
        internal InternalList Internal { get; }

        public int Count => Internal.Count;

        public ChowValue this[int index] => ApiValueConverter.ToApiClassObj(Internal[index]);

        public ChowList()
        {
            Internal = new InternalList();
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
