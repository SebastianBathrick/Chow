using System;

namespace Chow.Interpreter.Values
{
    public class ChowNone : ChowValue
    {
        const string NONE_STRING = "None";

        static readonly ChowValue _instance = new ChowNone();

        internal static ChowValue Instance => _instance;

        // Only one instance of ChowNone should exist
        ChowNone()
        {
            if (_instance == null)
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
