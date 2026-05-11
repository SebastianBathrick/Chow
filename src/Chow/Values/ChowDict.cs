using Chow.Interpreter.Values.Internal;

namespace Chow.Interpreter.Values
{
    public class ChowDict : ChowValue
    {
        internal InternalDict Internal { get; }

        public int Count => Internal.Count;

        public ChowValue this[ChowValue key]
            => ApiValueConverter.ToApiClassObj(Internal[ApiValueConverter.ToTaggedUnion(key)]);

        public ChowDict()
        {
            Internal = new InternalDict();
        }

        internal ChowDict(InternalDict wrapped)
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
