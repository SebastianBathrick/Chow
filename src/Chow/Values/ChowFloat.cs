namespace Chow.Interpreter.Values
{
    public class ChowFloat : ChowValue
    {
        private double _val;

        public ChowFloat(double val)
        {
            _val = val;
        }

        public override DataType As<DataType>()
        {
            if (typeof(DataType) == typeof(double))
            {
                return (DataType)(object)_val;
            }

            if (typeof(DataType) == typeof(int))
            {
                return (DataType)(object)(int)_val;
            }

            if (typeof(DataType) == typeof(bool))
            {
                return (DataType)(object)(_val != 0.0);
            }

            throw new InvalidCastException(GetType(), typeof(DataType), this);
        }

        public override bool Is<DataType>()
        {
            return typeof(DataType) == typeof(double);
        }

        public override string ToString() => _val.ToString();
    }
}
