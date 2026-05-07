namespace Chow.Interpreter.Values
{
    internal class ChowFloat : ChowValue
    {
        private float _floatValue;

        public override int IntegerValue { get => (int)_floatValue; }
        public override float FloatValue { get => _floatValue; }

        public override bool IsIntegerValue { get => false; }
        public override bool IsFloatValue { get => true; }

        public ChowFloat(float floatValue)
        {
            _floatValue = floatValue;
        }
    }
}
