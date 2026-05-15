using System;
namespace Chow.Interpreter.Values
{
    public class ChowNone : ChowValue
    {
        const string NONE_STRING = "None";

        // Only one instance of ChowNone should exist
        ChowNone()
        {
            if (Instance == null)
            {
                return;
            }

            throw new InvalidOperationException("Only one instance of ChowNone should exist.");
        }

        internal static ChowValue Instance { get; } = new ChowNone();

        public override TDataType AsType<TDataType>()
        {
            throw new InvalidCastException(GetType(), typeof(TDataType), this);
        }

        public override bool IsType<TDataType>()
        {
            return false;
        }

        public override string ToString()
        {
            return NONE_STRING;
        }
    }
}
