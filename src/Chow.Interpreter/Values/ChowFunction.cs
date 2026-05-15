namespace Chow.Interpreter.Values
{
    public class ChowFunction : ChowValue
    {
        public ChowFunction(object value)
        {
            Value = value;
        }
        public object Value { get; }

        public override TDataType AsType<TDataType>()
        {
            throw new InvalidCastException(GetType(), typeof(TDataType), this);
        }

        public override bool IsType<TDataType>()
        {
            return false;
        }

        public override string ToString()
        {
            return Value == null ? string.Empty : Value.ToString();
        }
    }
}
