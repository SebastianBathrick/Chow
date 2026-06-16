using Chow.Bytecode;
using Chow.SourceData;

namespace Chow.VM.FunctionCalls
{
    /// <summary>
    /// One slot on the <see cref="CallStack"/>. Pairs a <see cref="BytecodeChunk"/> being executed with its associated
    /// <see cref="Scope"/> and tracks the current instruction pointer.
    /// </summary>
    class StackFrame
    {
        const int INIT_INSTR_IDX = 0;

        // TODO: Start using "instr" to abbreviate "instruction"
        int _instrIdx;

        /// <summary>The bytecode bytecodeChunk this frame is executing.</summary>
        public BytecodeChunk BytecodeChunk { get; }

        /// <summary>The frame's scope: parentless for the module frame, parented to the closure's enclosing scope for any function frame.</summary>
        public Scope Scope { get; }

        /// <summary>The instruction at the current pointer.</summary>
        public Instruction CurrentInstr => BytecodeChunk[_instrIdx];

        /// <summary>True while the instruction pointer has not reached the end of the bytecodeChunk.</summary>
        public bool IsInstrToRun => _instrIdx < BytecodeChunk.InstructionCount;

        /// <summary>Source line number associated with the current instruction.</summary>
        public int CurrentLineNum => BytecodeChunk.GetLineIndex(_instrIdx);

        /// <summary>Creates a frame positioned at the first instruction.</summary>
        public StackFrame(BytecodeChunk bytecodeChunk, Scope scope)
        {
            BytecodeChunk = bytecodeChunk;
            Scope = scope;
            _instrIdx = INIT_INSTR_IDX;
        }

        /// <summary>Advances the instruction pointer by one.</summary>
        public void MoveToNextInstr()
        {
            _instrIdx++;
        }

        /// <summary>Sets the instruction pointer to <paramref name="instrIdx"/> and returns its previous value.</summary>
        public void JumpToInstr(int instrIdx)
        {
            _instrIdx = instrIdx;
        }
    }
}
