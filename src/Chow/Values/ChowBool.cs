namespace Chow.Interpreter.Values
{
    public class ChowBool : ChowValue
    {
        private const string TRUE_STRING = "True";
        private const string FALSE_STRING = "False";

        private bool _val;

        public ChowBool(bool val)
        {
            _val = val;
        }

        public override DataType As<DataType>()
        {
            if (typeof(DataType) == typeof(bool))
            {
                return (DataType)(object)_val;
            }

            if (typeof(DataType) == typeof(long))
            {
                return (DataType)(object)(_val ? 1L : 0L);
            }

            if (typeof(DataType) == typeof(double))
            {
                return (DataType)(object)(_val ? 1.0 : 0.0);
            }

            throw new InvalidCastException(GetType(), typeof(DataType), this);
        }

        public override bool Is<DataType>()
        {
            return typeof(DataType) == typeof(bool);
        }

        public override string ToString() => _val ? TRUE_STRING : FALSE_STRING;
    }
}
