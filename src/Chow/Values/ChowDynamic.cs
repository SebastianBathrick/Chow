namespace Chow.Interpreter.Values
{
    public class ChowDynamic : ChowValue
    {
        private object _val;

        public object Value => _val;


        public ChowDynamic(object val)
        {
            _val = val;
        }

        public override DataType As<DataType>()
        {
            if (_val is DataType value)
            {
                return value;
            }

            throw new InvalidCastException(GetType(), typeof(DataType), this);
        }

        public override bool IsTypeOf<DataType>()
        {
            return _val is DataType;
        }

        public override string ToString()
        {
            return _val == null ? string.Empty : _val.ToString();
        }
    }
}
