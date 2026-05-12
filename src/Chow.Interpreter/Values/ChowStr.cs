namespace Chow.Interpreter.Values
{
    public class ChowStr : ChowValue
    {
        public string Value { get; }

        public ChowStr(string val)
        {
            Value = val;
        }

        public override TDataType As<TDataType>()
        {
            if (typeof(TDataType) == typeof(bool))
            {
                return (TDataType)(object)(Value.Length != 0);
            }

            throw new InvalidCastException(GetType(), typeof(TDataType), this);
        }

        public override bool Is<TDataType>()
        {
            return false;
        }

        public override string ToString() => Value;
    }
}
