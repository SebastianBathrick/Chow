using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Bytecode
{
    readonly struct Operation
    {
        public OperationCode Code { get; }
        public int Operand { get; }

        public Operation(OperationCode type, int operand = -1)
        {
            Code = type;
            Operand = operand;
        }
    }
}
