namespace Chow.Interpreter.Values
{
    public class ChowInt : ChowValue
    {
        readonly long _val;

        public ChowInt(long val)
        {
            _val = val;
        }

        public override TDataType As<TDataType>()
        {
            if (typeof(TDataType) == typeof(long))
            {
                return (TDataType)(object)_val;
            }

            if (typeof(TDataType) == typeof(double))
            {
                return (TDataType)(object)(double)_val;
            }

            if (typeof(TDataType) == typeof(bool))
            {
                return (TDataType)(object)(_val != 0);
            }

            throw new InvalidCastException(GetType(), typeof(TDataType), this);
        }

        public override bool Is<TDataType>()
        {
            return typeof(TDataType) == typeof(long);
        }

        public override string ToString() => _val.ToString();
    }
}
