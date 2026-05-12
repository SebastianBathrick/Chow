namespace Chow.Interpreter.Values
{
    public class ChowDynamic : ChowValue
    {
        readonly object _val;

        public object Value => _val;


        public ChowDynamic(object val)
        {
            _val = val;
        }

        public override TDataType As<TDataType>()
        {
            if (_val is TDataType value)
            {
                return value;
            }

            throw new InvalidCastException(GetType(), typeof(TDataType), this);
        }

        public override bool Is<TDataType>()
        {
            return _val is TDataType;
        }

        public override string ToString()
        {
            return _val == null ? string.Empty : _val.ToString();
        }
    }
}
