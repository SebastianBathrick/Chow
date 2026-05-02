using Chow.Values;
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

        int _currIndex = 0;


        public int Count => _operations.Count;

        public Operation this[int index] => _operations[index];


        public TaggedUnion GetConstant(int index) => _constants[index];

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

        public override string ToString()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < _operations.Count; i++)
            {
                Operation op = _operations[i];
                sb.Append(i);
                sb.Append(": ");
                sb.Append(op.Code);

                if (op.Operand != -1)
                {
                    TaggedUnion constant = _constants[op.Operand];
                    sb.Append(' ');
                    sb.Append(op.Operand);
                    sb.Append(" (");
                    if (constant.IsInteger)
                    {
                        sb.Append("Int=");
                        sb.Append(constant.IntegerValue);
                    }
                    else if (constant.IsFloat)
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
