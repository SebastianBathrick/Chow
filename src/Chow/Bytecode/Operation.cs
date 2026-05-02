using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Bytecode
{
    readonly struct Operation
    {
        public OperationCode Type { get; }
        public int Operand { get; }

        public Operation(OperationCode type, int operand = -1)
        {
            Type = type;
            Operand = operand;
        }
    }
}
