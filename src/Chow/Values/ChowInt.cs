namespace Chow.Interpreter.Values
{
    public class ChowInt : ChowValue
    {
        private long _val;

        public ChowInt(long val)
        {
            _val = val;
        }

        public override DataType As<DataType>()
        {
            if (typeof(DataType) == typeof(long))
            {
                return (DataType)(object)_val;
            }

            if (typeof(DataType) == typeof(double))
            {
                return (DataType)(object)(double)_val;
            }

            if (typeof(DataType) == typeof(bool))
            {
                return (DataType)(object)(_val != 0);
            }

            throw new InvalidCastException(GetType(), typeof(DataType), this);
        }

        public override bool Is<DataType>()
        {
            return typeof(DataType) == typeof(long);
        }

        public override string ToString() => _val.ToString();
    }
}
