using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Interpreter
{
    public abstract class ChowValue
    {
        public abstract int IntegerValue { get; set; }
        public abstract float FloatValue { get; set; }

        public abstract bool IsIntegerValue { get; set; }
        public abstract float IsFloatValue { get; set; }
    }
}

