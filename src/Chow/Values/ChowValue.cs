using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Interpreter.Values
{
    public abstract class ChowValue
    {
        public ChowValue None => ChowNone.Instance;

        public bool IsNone => this == ChowNone.Instance;

        public abstract int IntegerValue { get; }
        public abstract float FloatValue { get; }
        public abstract bool IsIntegerValue { get; }
        public abstract bool IsFloatValue { get; }

    }
}

