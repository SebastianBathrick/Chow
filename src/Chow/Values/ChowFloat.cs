namespace Chow.Interpreter.Values
{
    internal class ChowFloat : ChowValue
    {
        private float _floatValue;

        public override int IntValue { get => (int)_floatValue; }
        public override float FloatValue { get => _floatValue; }
        public override bool BoolValue { get => _floatValue != 0f; }

        public override bool IsIntValue { get => false; }
        public override bool IsFloatValue { get => true; }
        public override bool IsBoolValue { get => false; }

        public ChowFloat(float floatValue)
        {
            _floatValue = floatValue;
        }

        public override string ToString() => _floatValue.ToString();
    }
}
