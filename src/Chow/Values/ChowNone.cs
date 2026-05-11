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
            if (_instance == null)
            {
                return;
            }

            throw new InvalidOperationException("Only one instance of ChowNone should exist.");
        }

        public override DataType As<DataType>()
        {
            throw new InvalidCastException(GetType(), typeof(DataType), this);
        }

        public override bool Is<DataType>()
        {
            return false;
        }

        public override string ToString() => NONE_STRING;
    }
}
