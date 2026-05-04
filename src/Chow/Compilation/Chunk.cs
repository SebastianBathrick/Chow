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

        public void PushOperation(OperationCode operationType, int lineNumber, int operand = -1)
        {
            _operations.Add(new Operation(operationType, operand));
            _operationLineNums.Add(lineNumber);
        }

        #region Constant Methods

        public TaggedUnion GetConstant(int index) => _consts[index];

        public int RegisterConstant(TaggedUnion newConst)
        {
            int constIndex = FindConstantIndex(newConst);

            if (constIndex < 0)
            {
                constIndex = _consts.Count;
                _consts.Add(newConst);
            }

            return constIndex;
        }

        int FindConstantIndex(TaggedUnion constant) => _consts.IndexOf(constant);

        #endregion

        #region Variable Name Methods

        // TEMPORARY LOGIC: This public API is intended to abstract how variable identifiers are stored and accessed.
        // This public API will remain the same, but their function logic will change later in developement. Currently
        // variables are internally stored the exact same way as constants, and internally retrieved the exact same way
        // as constants due to time constraints. However, all variable-name related logic will be accessed by the client
        // via dedicated variable methods, so when variable-names are stored differently in Chunk, no client code will 
        // need to be changed.

        // NOTE: Making the GetConstant call does return a new struct, and that is slower, but it is temporary and I want
        // them to work identically as constants for the time being. Less code to manage.
        public string GetVariableName(int index) => GetConstant(index).StringValue;

        // This is one piece of functionality that constant will never have publically (still going to change internally for variables)
        public int FindVariableIndex(string varName) => FindConstantIndex(new TaggedUnion(varName));

        public int RegisterVariableName(string varName) => RegisterConstant(new TaggedUnion(varName));

        #endregion

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
