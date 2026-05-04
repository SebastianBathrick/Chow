using Chow.Interpreter.Values;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Interpreter.Jit
{
    class Chunk
    {
        List<Operation> _operations = new List<Operation>();
        List<TaggedUnion> _consts = new List<TaggedUnion>();
        List<int> _operationLineNums = new List<int>();



        public int Count => _operations.Count;

        public Operation this[int index] => _operations[index];


        public TaggedUnion GetConstant(int index) => _consts[index];

        public int AddConstant(TaggedUnion newConst)
        {
            int constIndex = _consts.IndexOf(newConst);

           if (constIndex < 0)
            {
                constIndex = _consts.Count;
                _consts.Add(newConst);
            }

            return constIndex;
        }

        public void PushOperation(OperationCode operationType, int lineNumber, int operand = -1)
        {
            _operations.Add(new Operation(operationType, operand));
            _operationLineNums.Add(lineNumber);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine("Constants:");
            for (int i = 0; i < _consts.Count; i++)
            {
                TaggedUnion constant = _consts[i];

                sb.Append("  ");
                sb.Append(i);
                sb.Append(": ");

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

                sb.AppendLine();
            }

            sb.AppendLine("Operations:");
            for (int i = 0; i < _operations.Count; i++)
            {
                Operation op = _operations[i];

                sb.Append("  ");
                sb.Append(i);
                sb.Append(": ");
                sb.Append(op.Code);

                if (op.Operand != -1)
                {
                    TaggedUnion constant = _consts[op.Operand];

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
