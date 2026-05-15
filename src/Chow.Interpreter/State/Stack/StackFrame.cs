using Chow.Interpreter.Bytecode;
using Chow.Interpreter.State.Scopes;

namespace Chow.Interpreter.State.Stack
{
    /// <summary>
    /// One slot on the <see cref="CallStack"/>. Pairs a <see cref="Chunk"/> being executed with its associated
    /// <see cref="Scope"/> and tracks the current instruction pointer.
    /// </summary>
    class StackFrame
    {
        const int INIT_INSTR_IDX = 0;

        // TODO: Start using "instr" to abbreviate "instruction"
        int _instrIdx;

        /// <summary>The bytecode chunk this frame is executing.</summary>
        public Chunk Chunk { get; }

        /// <summary>The frame's scope: a <see cref="ModuleScope"/> for the module frame, a <see cref="LocalScope"/> for any function frame.</summary>
        public IScope Scope { get; }

        /// <summary>The instruction at the current pointer.</summary>
        public Instruction CurrentInstr => Chunk[_instrIdx];

        /// <summary>True while the instruction pointer has not reached the end of the chunk.</summary>
        public bool IsInstrToRun => Chunk.InstructionCount != _instrIdx;

        /// <summary>Source line number associated with the current instruction.</summary>
        public int CurrentLineNum => Chunk.GetInstructionLineIndex(_instrIdx);

        /// <summary>Creates a frame positioned at the first instruction.</summary>
        public StackFrame(Chunk chunk, IScope scope)
        {
            Chunk = chunk;
            Scope = scope;
            _instrIdx = INIT_INSTR_IDX;
        }

        /// <summary>Advances the instruction pointer by one.</summary>
        public void MoveToNextInstr()
        {
            _instrIdx++;
        }

        /// <summary>Sets the instruction pointer to <paramref name="instrIdx"/> and returns its previous value.</summary>
        public int JumpToInstr(int instrIdx)
        {
            var prevInstrIdx = _instrIdx;
            _instrIdx = instrIdx;
            return prevInstrIdx;
        }
    }
}
