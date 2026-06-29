using System.Collections.Generic;
using Chow.SourceData;

namespace Chow.Bytecode
{
    class BytecodeChunk
    {
        const int NoOperand = -1;

        #region Fields
        
        readonly List<Instruction> _instrList = new List<Instruction>();
        readonly List<SourceValue> _constPool = new List<SourceValue>();

        readonly List<string> _varNames = new List<string>();
        readonly List<int> _instrLines = new List<int>();

        #endregion
        
        #region Properties
        
        public int InstructionCount => _instrList.Count;

        public Instruction this[int index] => _instrList[index];

        #endregion
        
        #region Instruction Methods

        public void Add(OperationCode code, int line, int operand = NoOperand)
        {
            _instrList.Add(new Instruction(code, operand));
            _instrLines.Add(line);
        }

        public void PatchOperand(int insrIdx, int operand)
        {
            _instrList[insrIdx] = new Instruction(_instrList[insrIdx].Code, operand);
        }

        public int GetLineIndex(int instrIdx)
        {
            return _instrLines[instrIdx];
        }

        #endregion

        #region Constant Pool Methods

        public SourceValue ReadConstant(int operand)
        {
            return _constPool[operand];
        }

        public int RegisterConstant(SourceValue newConst)
        {
            var constIndex = _constPool.IndexOf(newConst);

            if (constIndex >= 0)
            {
                return constIndex;
            }

            constIndex = _constPool.Count;
            _constPool.Add(newConst);
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
