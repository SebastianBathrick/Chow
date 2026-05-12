namespace Chow.Interpreter.Values
{
    public class ChowFloat : ChowValue
    {
        readonly double _val;

        public ChowFloat(double val)
        {
            _val = val;
        }

        public override TDataType As<TDataType>()
        {
            if (typeof(TDataType) == typeof(double))
            {
                return (TDataType)(object)_val;
            }

            if (typeof(TDataType) == typeof(long))
            {
                return (TDataType)(object)(long)_val;
            }

            if (typeof(TDataType) == typeof(bool))
            {
                return (TDataType)(object)(_val != 0.0);
            }

            throw new InvalidCastException(GetType(), typeof(TDataType), this);
        }

        public override bool Is<TDataType>()
        {
            return typeof(TDataType) == typeof(double);
        }

        public override string ToString() => _val.ToString();
    }
}
