using System;

namespace Chow.Interpreter.Values
{
    public class ChowNone : ChowValue
    {
        private const string NONE_STRING = "None";

        private static ChowValue _instance = new ChowNone();

        internal static ChowValue Instance
        {
            get { return _instance; }
        }

        // Only one instance of ChowNone should exist
        private ChowNone()
        {
            if (_instance != null)
            {
                throw new InvalidOperationException("Only one instance of ChowNone should exist.");
            }
        }

        // Throw ChowConversionException
        public override int IntegerValue { get => throw new InvalidCastException(GetType(), typeof(int), this); }

        public override float FloatValue { get => throw new InvalidCastException(GetType(), typeof(float), this); }

        public override bool IsIntegerValue
        {
            get { return false; }
        }

        public override bool IsFloatValue
        {
            get { return false; }
        }

        public override string ToString() => NONE_STRING;
    }
}
