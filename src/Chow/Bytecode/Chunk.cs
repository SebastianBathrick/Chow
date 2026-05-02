using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Bytecode
{
    class Chunk
    {
        List<Operation> _operations = new List<Operation>();
        List<TaggedUnion> _constants = new List<TaggedUnion>(); 
        List<int> _operationLineNums = new List<int>();

        public int AddConstant(TaggedUnion newConstant)
        {
            _constants.Add(newConstant);
            return _constants.Count - 1; // Provide an index to use as an Operation operand
        }

        public void PushOperation(OperationCode operationType, int lineNumber, int operand = -1)
        {
            _operations.Add(new Operation(operationType, operand));
            _operationLineNums.Add(lineNumber);
        }

        public int Count => _operations.Count;

        public Operation this[int index] => _operations[index];

        public override string ToString()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < _operations.Count; i++)
            {
                Operation op = _operations[i];
                sb.Append(i);
                sb.Append(": ");
                sb.Append(op.Type);

                if (op.Operand != -1)
                {
                    TaggedUnion constant = _constants[op.Operand];
                    sb.Append(' ');
                    sb.Append(op.Operand);
                    sb.Append(" (");
                    if (constant.Type == TaggedUnionType.Integer)
                    {
                        sb.Append("Int=");
                        sb.Append(constant.IntegerValue);
                    }
                    else if (constant.Type == TaggedUnionType.Float)
                    {
                        sb.Append("Float=");
                        sb.Append(constant.FloatValue);
                    }
                    sb.Append(')');
                }

                if (i < _operations.Count - 1)
                {
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }
    }
}
