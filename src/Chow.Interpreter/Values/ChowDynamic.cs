namespace Chow.Interpreter.Values
{
    public class ChowDynamic : ChowValue
    {
        public object Value { get; }


        public ChowDynamic(object val)
        {
            Value = val;
        }

        public override TDataType AsType<TDataType>()
        {
            if (Value is TDataType value)
            {
                return value;
            }

            throw new InvalidCastException(GetType(), typeof(TDataType), this);
        }

        public override bool IsType<TDataType>()
        {
            return Value is TDataType;
        }

        public override string ToString()
        {
            return Value == null ? string.Empty : Value.ToString();
        }
    }
}
