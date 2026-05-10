namespace Chow.Interpreter.Values
{
    internal class ChowInteger : ChowValue
    {
        private int _val;

        public ChowInteger(int val)
        {
            _val = val;
        }

        public override DataType As<DataType>()
        {
            if (typeof(DataType) == typeof(int))
            {
                return (DataType)(object)_val;
            }

            if (typeof(DataType) == typeof(float))
            {
                return (DataType)(object)(float)_val;
            }

            if (typeof(DataType) == typeof(bool))
            {
                return (DataType)(object)(_val != 0);
            }

            throw new InvalidCastException(GetType(), typeof(DataType), this);
        }

        public override bool ContainsType<DataType>()
        {
            return typeof(DataType) == typeof(int);
        }

        public override string ToString() => _val.ToString();
    }
}
