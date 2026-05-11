using Chow.Interpreter.Values;
using System.Collections.Generic;
using System.Text;

namespace Chow.Interpreter.Compilation
{
    class Chunk
    {
        static int _nextIdCounter = 0;
        static object _nextIdCounterLock = new object();

        int _id = 0;

        List<Instruction> _instrs;
        List<TaggedUnion> _consts;

        List<string> _varNames;
        List<int> _instrLineNums;

        public int Id => _id;

        public int Count => _instrs.Count;

        public Instruction this[int index] => _instrs[index];

        public Chunk()
        {
            _instrs = new List<Instruction>();
            _consts = new List<TaggedUnion>();
            _varNames = new List<string>();
            _instrLineNums = new List<int>();

            lock(_nextIdCounterLock)
            {
                _id = _nextIdCounter;
                ++_nextIdCounter;
            }
        }

        public static bool AreSameChunks(Chunk c1, Chunk c2)
        {
            return c1._id == c2._id;
        }

        #region Instruction Methods

        public void AddInstr(OperationCode code, int lineNumber, int operand = -1)
        {
            _instrs.Add(new Instruction(code, operand));
            _instrLineNums.Add(lineNumber);
        }

        public void PatchInstrOperand(int idx, int operand)
        {
            _instrs[idx] = new Instruction(_instrs[idx].Code, operand);
        }

        public int GetInstrLineNum(int instrIdx)
        {
            return _instrLineNums[instrIdx];
        }

        #endregion

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

        public bool IsVariableName(string name)
        {
            return _varNames.Contains(name);
        }

        public string ReadVariableName(int operand)
        {
            return _varNames[operand];
        }

        public int FindVariableName(string name)
        {
            return _varNames.IndexOf(name);
        }

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
            for (int i = 0; i < _instrs.Count; i++)
            {
                Instruction op = _instrs[i];

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

                if (i < _instrs.Count - 1)
                {
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        static void AppendConstant(StringBuilder sb, TaggedUnion constant)
        {
            if (constant.IsInt)
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
