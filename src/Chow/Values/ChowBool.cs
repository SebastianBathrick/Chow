namespace Chow.Interpreter.Values
{
    internal class ChowBool : ChowValue
    {
        private const string TRUE_STRING = "True";
        private const string FALSE_STRING = "False";

        private bool _val;

        public override int IntValue { get => _val ? 1 : 0; }
        public override float FloatValue { get => _val ? 1f : 0f; }
        public override bool BoolValue { get => _val; }

        public override bool IsIntValue { get => false; }
        public override bool IsFloatValue { get => false; }
        public override bool IsBoolValue { get => true; }

        public ChowBool(bool val)
        {
            _val = val;
        }

        public override string ToString() => _val ? TRUE_STRING : FALSE_STRING;
    }
}
