namespace Chow.Interpreter.Values
{
    public abstract class ChowValue
    {
        public static ChowValue None => ChowNone.Instance;

        public bool IsNone => this == None;

        public abstract int IntValue { get; }
        public abstract float FloatValue { get; }
        public abstract bool BoolValue { get; }
        public abstract bool IsIntValue { get; }
        public abstract bool IsFloatValue { get; }
        public abstract bool IsBoolValue {  get; }

        public abstract override string ToString();
    }
}

