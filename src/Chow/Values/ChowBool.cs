namespace Chow.Interpreter.Values
{
    public class ChowBool : ChowValue
    {
        const string TRUE_STRING = "True";
        const string FALSE_STRING = "False";

        readonly bool _val;

        public ChowBool(bool val)
        {
            _val = val;
        }

        public override TDataType As<TDataType>()
        {
            if (typeof(TDataType) == typeof(bool))
            {
                return (TDataType)(object)_val;
            }

            if (typeof(TDataType) == typeof(long))
            {
                return (TDataType)(object)(_val ? 1L : 0L);
            }

            if (typeof(TDataType) == typeof(double))
            {
                return (TDataType)(object)(_val ? 1.0 : 0.0);
            }

            throw new InvalidCastException(GetType(), typeof(TDataType), this);
        }

        public override bool Is<TDataType>()
        {
            return typeof(TDataType) == typeof(bool);
        }

        public override string ToString() => _val ? TRUE_STRING : FALSE_STRING;
    }
}
