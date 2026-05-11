namespace Chow.Interpreter.Values
{
    public class ChowInt : ChowValue
    {
        private int _val;

        public ChowInt(int val)
        {
            _val = val;
        }

        public override DataType As<DataType>()
        {
            if (typeof(DataType) == typeof(int))
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
            return typeof(DataType) == typeof(int);
        }

        public override string ToString() => _val.ToString();
    }
}
