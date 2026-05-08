using Chow.Interpreter.Values;
using System.Collections.Generic;
using System.Text;

namespace Chow.Interpreter.Compilation
{
    class Chunk
    {
        List<Instruction> _opList = new List<Instruction>();
        List<TaggedUnion> _consts = new List<TaggedUnion>();
        List<string> _varNames = new List<string>();
        List<int> _opLineNums = new List<int>();

        public int Count => _opList.Count;

        public Instruction this[int index] => _opList[index];

        public void PushOperation(OperationCode operationType, int lineNumber, int operand = -1)
        {
            _opList.Add(new Instruction(operationType, operand));
            _opLineNums.Add(lineNumber);
        }

        public int GetOperationLineNumber(int operand)
        {
            return _opLineNums[operand];
        }

        #region Constant Methods

        public TaggedUnion ReadConstant(int operand) => _consts[operand];

        /// <summary>
        /// Stores a constant value and returns an integer for use as an operand assigned to <see cref="Instruction"/> instance(s).
        /// </summary>
        /// <param name="newConst">TaggedUnion containing a constant primitive value.</param>
        /// <returns>Integer representing the operand used to read the constant at runtime.</returns>
        /// <remarks>If an existing constant has the same value as <paramref name="newConst"/> then the operand for 
        /// that existing constant will be returned. Otherwise, the new constant is stored and a new operand is returned</remarks>
        public int RegisterConstant(TaggedUnion newConst)
        {
            int constIndex = FindConstantIndex(newConst);

            if (constIndex >= 0)
            {
                return constIndex;
            }

            constIndex = _consts.Count;
            _consts.Add(newConst);
            return constIndex;
        }

        // The constant list's index is only to be refered to as "operand" in the public API to hide interal functionality
        int FindConstantIndex(TaggedUnion constant) => _consts.IndexOf(constant);

        #endregion

        #region Variable Name Methods

        public string ReadVariableName(int operand) => _varNames[operand];

        public int FindVariableName(string varName) => _varNames.IndexOf(varName);

        /// <summary>
        /// Used to register a variable name compile-time and return an operand for use in <see cref="Instruction"/> instance(s)
        /// that declare or reference that variable.
        /// </summary>
        /// <param name="varName">Variable name to register.</param>
        /// <returns>If a variable name equal to <paramref name="varName"/> is already registered, the operand for the
        /// existing entry is returned. Otherwise, the new variable name is stored and a new operand is returned.</returns>
        /// <remarks>This is ONLY for storing variable names COMPILE-TIME. NOT for storing variable names runtime, AND NOT NEVER
        /// for storing variable values. </remarks>
        public int RegisterVariableName(string varName)
        {
            int existing = FindVariableName(varName);

            if (existing >= 0)
            {
                return existing;
            }

            int operand = _varNames.Count;
            _varNames.Add(varName);
            return operand;
        }

        #endregion

        #region ToString Methods

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine("Constants:");
            for (int i = 0; i < _consts.Count; i++)
            {
                sb.Append("  ");
                sb.Append(i);
                sb.Append(": ");
                AppendConstant(sb, _consts[i]);
                sb.AppendLine();
            }

            sb.AppendLine("Variables:");
            for (int i = 0; i < _varNames.Count; i++)
            {
                sb.Append("  ");
                sb.Append(i);
                sb.Append(": ");
                sb.Append(_varNames[i]);
                sb.AppendLine();
            }

            sb.AppendLine("Operations:");
            for (int i = 0; i < _opList.Count; i++)
            {
                Instruction op = _opList[i];

                sb.Append("  ");
                sb.Append(i);
                sb.Append(": ");
                sb.Append(op.Code);

                if (op.Operand != -1)
                {
                    sb.Append(' ');
                    sb.Append(op.Operand);
                    sb.Append(" (");
                    AppendOperandTarget(sb, op);
                    sb.Append(')');
                }

                if (i < _opList.Count - 1)
                {
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        static void AppendConstant(StringBuilder sb, TaggedUnion constant)
        {
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
            else if (constant.IsString)
            {
                sb.Append("String=");
                sb.Append(constant.StringValue);
            }
            else if (constant.IsBoolean)
            {
                sb.Append("Bool=");
                sb.Append(constant.BooleanValue);
            }
        }

        void AppendOperandTarget(StringBuilder sb, Instruction op)
        {
            switch (op.Code)
            {
                case OperationCode.PushConstant:
                    AppendConstant(sb, _consts[op.Operand]);
                    break;
                case OperationCode.AssignOrDeclareVariable:
                case OperationCode.PushVariableValue:
                    sb.Append("Var=");
                    sb.Append(_varNames[op.Operand]);
                    break;
            }
        }

        #endregion
    }
}
