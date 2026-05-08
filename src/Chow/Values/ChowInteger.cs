namespace Chow.Interpreter.Values
{
    internal class ChowInteger : ChowValue
    {
        private int _integerValue;

        public override int IntegerValue { get => _integerValue; }
        public override float FloatValue { get => (float)_integerValue; }
        public override bool BoolValue { get => _integerValue != 0; }

        public override bool IsIntegerValue { get => true; }
        public override bool IsFloatValue { get => false; }
        public override bool IsBoolValue { get => false; }

        public ChowInteger(int integerValue)
        {
            _integerValue = integerValue;
        }

        public override string ToString() => _integerValue.ToString();
    }
}
