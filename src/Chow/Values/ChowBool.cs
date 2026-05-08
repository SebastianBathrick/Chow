namespace Chow.Interpreter.Values
{
    internal class ChowBool : ChowValue
    {
        private const string TRUE_STRING = "True";
        private const string FALSE_STRING = "False";

        private bool _boolValue;

        public bool BooleanValue { get => _boolValue; }

        public override int IntegerValue { get => _boolValue ? 1 : 0; }
        public override float FloatValue { get => _boolValue ? 1f : 0f; }

        public override bool IsIntegerValue { get => false; }
        public override bool IsFloatValue { get => false; }

        public ChowBool(bool boolValue)
        {
            _boolValue = boolValue;
        }

        public override string ToString() => _boolValue ? TRUE_STRING : FALSE_STRING;
    }
}
