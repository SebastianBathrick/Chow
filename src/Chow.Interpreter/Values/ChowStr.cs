namespace Chow.Interpreter.Values
{
    public class ChowStr : ChowValue
    {
        public ChowStr(string val)
        {
            Value = val;
        }
        public string Value { get; }

        public override TDataType AsType<TDataType>()
        {
            if (typeof(TDataType) == typeof(bool))
            {
                return (TDataType)(object)(Value.Length != 0);
            }

            throw new InvalidCastException(GetType(), typeof(TDataType), this);
        }

        public override bool IsType<TDataType>()
        {
            return false;
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
