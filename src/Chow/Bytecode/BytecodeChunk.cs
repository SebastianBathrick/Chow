using System.Collections.Generic;
using Chow.SourceData;

namespace Chow.Bytecode
{
    class BytecodeChunk
    {
        const int NoOperand = -1;

        readonly List<Instruction> _instructions;
        readonly List<SourceValue> _constantPool;

        readonly List<string> _varNames;
        readonly List<int> _instrLines;

        public int InstructionCount => _instructions.Count;

        public Instruction this[int index] => _instructions[index];

        public BytecodeChunk()
        {
            _instructions = new List<Instruction>();
            _constantPool = new List<SourceValue>();
            _varNames = new List<string>();
            _instrLines = new List<int>();
        }

        #region Instruction Methods

        public void Add(OperationCode code, int line, int operand = NoOperand)
        {
            _instructions.Add(new Instruction(code, operand));
            _instrLines.Add(line);
        }

        public void PatchOperand(int insrIdx, int operand)
        {
            _instructions[insrIdx] = new Instruction(_instructions[insrIdx].Code, operand);
        }

        public int GetLineIndex(int instrIdx)
        {
            return _instrLines[instrIdx];
        }

        #endregion

        #region Constant Methods

        public SourceValue ReadConstant(int operand)
        {
            return _constantPool[operand];
        }

        public int RegisterConstant(SourceValue newConst)
        {
            var constIndex = _constantPool.IndexOf(newConst);

            if (constIndex >= 0)
            {
                return constIndex;
            }

            constIndex = _constantPool.Count;
            _constantPool.Add(newConst);
            return constIndex;
        }

        #endregion

        #region Variable Name Methods

        public string GetVariableName(int operand)
        {
            return _varNames[operand];
        }

        public int FindVariableName(string name)
        {
            return _varNames.IndexOf(name);
        }

        public int RegisterVariableName(string varName)
        {
            var existing = FindVariableName(varName);

            if (existing >= 0)
            {
                return existing;
            }

            var operand = _varNames.Count;
            _varNames.Add(varName);
            return operand;
        }

        #endregion
    }
}
