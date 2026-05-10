namespace Chow.Interpreter.Values
{
    internal class ChowFloat : ChowValue
    {
        private float _val;

        public ChowFloat(float val)
        {
            _val = val;
        }

        public override DataType As<DataType>()
        {
            if (typeof(DataType) == typeof(float))
            {
                return (DataType)(object)_val;
            }

            if (typeof(DataType) == typeof(int))
            {
                return (DataType)(object)(int)_val;
            }

            if (typeof(DataType) == typeof(bool))
            {
                return (DataType)(object)(_val != 0f);
            }

            throw new InvalidCastException(GetType(), typeof(DataType), this);
        }

        public override bool IsTypeOf<DataType>()
        {
            return typeof(DataType) == typeof(float);
        }

        public override string ToString() => _val.ToString();
    }
}
