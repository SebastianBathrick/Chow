namespace Chow.Interpreter.Values
{
    public abstract class ChowValue
    {
        public static ChowValue None => ChowNone.Instance;

        public bool IsNone => this == None;

        public abstract int IntegerValue { get; }
        public abstract float FloatValue { get; }
        public abstract bool IsIntegerValue { get; }
        public abstract bool IsFloatValue { get; }

        public abstract override string ToString();
    }
}

