using System;

namespace Chow.Interpreter.Values
{
    public class ChowNone : ChowValue
    {
        const string NONE_STRING = "None";

        internal static ChowValue Instance { get; } = new ChowNone();

        // Only one instance of ChowNone should exist
        ChowNone()
        {
            if (Instance == null)
            {
                return;
            }

            throw new InvalidOperationException("Only one instance of ChowNone should exist.");
        }

        public override TDataType As<TDataType>()
        {
            throw new InvalidCastException(GetType(), typeof(TDataType), this);
        }

        public override bool Is<TDataType>()
        {
            return false;
        }

        public override string ToString() => NONE_STRING;
    }
}
