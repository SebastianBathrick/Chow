namespace Chow.Interpreter.Values
{
    public class ChowStr : ChowValue
    {
        readonly string _val;

        public string Value => _val;

        public ChowStr(string val)
        {
            _val = val;
        }

        public override TDataType As<TDataType>()
        {
            if (typeof(TDataType) == typeof(bool))
            {
                return (TDataType)(object)(_val.Length != 0);
            }

            throw new InvalidCastException(GetType(), typeof(TDataType), this);
        }

        public override bool Is<TDataType>()
        {
            return false;
        }

        public override string ToString() => _val;
    }
}
