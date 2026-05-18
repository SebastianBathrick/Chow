using System.Collections.Generic;
using Chow.Interpreter.Values;
namespace Chow.Interpreter.Bytecode
{
    class Chunk
    {
        const int NO_OPERAND = -1;

        readonly List<Instruction> _instructions;
        readonly List<ChowValue> _constantPool;

        readonly List<string> _varNames;
        readonly List<int> _instrLines;

        /// <summary>The total number of bytecode instructions stored in this Chunk.</summary>
        public int InstructionCount => _instructions.Count;

        /// <summary>Returns the instruction stored in this Chunk at the provided index.</summary>
        /// <param name="index">The index of the instruction to retrieve.</param>
        /// <returns>The instruction at the specified index.</returns>
        public Instruction this[int index] => _instructions[index];

        /// <summary>Initializes a new Chunk without any instructions, constants, or variable names.</summary>
        public Chunk()
        {
            _instructions = new List<Instruction>();
            _constantPool = new List<ChowValue>();
            _varNames = new List<string>();
            _instrLines = new List<int>();
        }

        #region Instruction Methods

        /// <summary>Creates and adds a new <see cref="Instruction"/> with the provided operation code, operand, and line number </summary>
        /// <param name="code">Operation code associated with the instruction's logic in the <see cref="Interpreter.VirtualMachine"/></param>
        /// <param name="line">The line number in the source code associated with this instruction.</param>
        /// <param name="operand">Optional operand for the instruction, default is -1.</param>
        public void AddInstruction(OperationCode code, int line, int operand = NO_OPERAND)
        {
            _instructions.Add(new Instruction(code, operand));
            _instrLines.Add(line);
        }

        /// <summary>Replaces the operand of the <see cref="Instruction"/> at the provided index, preserving its operation code.</summary>
        /// <param name="idx">The index of the instruction to patch.</param>
        /// <param name="operand">The new operand value to assign to the instruction.</param>
        public void PatchInstructionOperand(int idx, int operand)
        {
            _instructions[idx] = new Instruction(_instructions[idx].Code, operand);
        }

        /// <summary>Returns the source code line number associated with the instruction at the provided index.</summary>
        /// <param name="instrIdx">The index of the instruction whose line number is to be retrieved.</param>
        /// <returns>The line number in the source code associated with the specified instruction.</returns>
        public int GetInstructionLineIndex(int instrIdx)
        {
            return _instrLines[instrIdx];
        }

        #endregion

        #region Constant Methods

        /// <summary>Returns the constant value stored at the provided operand index in the constant pool.</summary>
        /// <param name="operand">The operand index of the constant to retrieve.</param>
        /// <returns>The <see cref="ChowValue"/> constant at the specified operand index.</returns>
        public ChowValue ReadConstant(int operand)
        {
            return _constantPool[operand];
        }

        /// <summary>
        /// Stores a new constant in the constant pool and returns its pool index. The index is for use as an operand
        /// assigned to <see cref="Instruction"/> instance(s).
        /// </summary>
        /// <param name="newConst">ChowValue containing a constant primitive value.</param>
        /// <returns>Integer representing the operand used to read the constant at runtime.</returns>
        /// <remarks>If an existing constant has the same value as <paramref name="newConst"/> then the operand for 
        /// that existing constant will be returned. Otherwise, the new constant is stored and a new operand is returned</remarks>
        public int RegisterConstant(ChowValue newConst)
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

        /// <summary>Returns the variable name stored at the provided operand that represents the variable name index.</summary>
        /// <param name="operand">The operand representing the index of the variable name to retrieve.</param>
        /// <returns>The variable name at the specified index.</returns>
        public string ReadVariableName(int operand)
        {
            return _varNames[operand];
        }

        /// <summary>Returns the index of the provided variable name in the variable name pool, or -1 if the name is not registered.</summary>
        /// <param name="name">The variable name to look up.</param>
        /// <returns>The operand index of the variable name, or -1 if the name is not registered.</returns>
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
        /// <remarks>This is ONLY for storing variable names COMPILE-TIME. NOT for storing variable names runtime, AND NEVER
        /// for storing variable values. </remarks>
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
