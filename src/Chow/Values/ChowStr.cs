namespace Chow.Interpreter.Values
{
    public class ChowStr : ChowValue
    {
        private string _val;

        public string Value => _val;

        public ChowStr(string val)
        {
            _val = val;
        }

        public override DataType As<DataType>()
        {
            if (typeof(DataType) == typeof(bool))
            {
                return (DataType)(object)(_val.Length != 0);
            }

            throw new InvalidCastException(GetType(), typeof(DataType), this);
        }

        public override bool IsTypeOf<DataType>()
        {
            return false;
        }

        public override string ToString() => _val;
    }
}
