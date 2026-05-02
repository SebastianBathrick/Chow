using System;
using System.Collections.Generic;
using System.Text;

namespace Chow
{
    internal class ChowInteger : ChowValue
    {
        private int _integerValue;

        public override int IntegerValue { get => _integerValue; set => _integerValue = value; }
        public override float FloatValue { get => (float)_integerValue; set => _integerValue = (int)value; }

        public override bool IsIntegerValue { get => true; set { } }
        public override float IsFloatValue { get => 0f; set { } }

        public ChowInteger(int integerValue)
        {
            _integerValue = integerValue;
        }
    }
}
