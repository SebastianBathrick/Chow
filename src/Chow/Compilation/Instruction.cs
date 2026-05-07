using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Interpreter.Compilation
{
    readonly struct Instruction
    {
        public OperationCode Code { get; }
        public int Operand { get; }

        public Instruction(OperationCode type, int operand = -1)
        {
            Code = type;
            Operand = operand;
        }
    }
}
