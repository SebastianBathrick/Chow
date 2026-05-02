using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace Chow.Bytecode
{
    readonly struct Operation
    {
        public OperandType Type { get; }
        public int Operand { get; }

        public Operation(OperandType type, int operand)
        {
            Type = type;
            Operand = operand;
        }
    }
}
