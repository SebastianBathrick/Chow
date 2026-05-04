using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Interpreter
{
    internal class ChowFloat : ChowValue
    {
        private float _floatValue;

        public override int IntegerValue { get => (int)_floatValue; set => _floatValue = (float)value; }
        public override float FloatValue { get => _floatValue; set => _floatValue = value; }

        public override bool IsIntegerValue { get => false; set { } }
        public override float IsFloatValue { get => 1f; set { } }

        public ChowFloat(float floatValue)
        {
            _floatValue = floatValue;
        }
    }
}
