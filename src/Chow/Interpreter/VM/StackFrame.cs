using Chow.Bytecode;
using Chow.SourceData;

namespace Chow.Interpreter.VM
{
    /// <summary>
    /// One slot on the <see cref="CallStack"/>. Pairs a <see cref="BytecodeChunk"/> being executed with
    /// its associated
    /// <see cref="Scope"/> and tracks the current instruction pointer.
    /// </summary>
    class StackFrame
    {
        const int InitInstrIdx = 0;

        // TODO: Start using "instr" to abbreviate "instruction"
        int _instrIdx;

        /// <summary>The bytecode bytecodeChunk this frame is executing.</summary>
        public BytecodeChunk BytecodeChunk { get; }

        /// <summary>
        /// The frame's scope: parentless for the module frame, parented to the closure's enclosing
        /// scope for any function frame.
        /// </summary>
        public Scope Scope { get; }

        /// <summary>The instruction at the current pointer.</summary>
        public Instruction CurrentInstr => BytecodeChunk[_instrIdx];

        /// <summary>True while the instruction pointer has not reached the end of the bytecodeChunk.</summary>
        public bool IsInstrToRun => _instrIdx < BytecodeChunk.InstructionCount;

        /// <summary>Source line number associated with the current instruction.</summary>
        public int CurrentLineNum => BytecodeChunk.GetLineIndex(_instrIdx);

        /// <summary>
        /// The value the call site yields instead of this frame's return value. Set when the frame
        /// is a constructor call: <c>__init__</c> returns None, but <c>Point(1, 2)</c> must evaluate
        /// to the new instance. Meaningful only when <see cref="HasConstructionResult"/> is
        /// <see langword="true"/>.
        /// </summary>
        public SourceValue ConstructionResult { get; private set; }

        /// <summary>
        /// Whether this frame carries a <see cref="ConstructionResult"/>. Tracked separately because
        /// None is itself a legitimate return value.
        /// </summary>
        public bool HasConstructionResult { get; private set; }

        /// <summary>Creates a frame positioned at the first instruction.</summary>
        public StackFrame(BytecodeChunk bytecodeChunk, Scope scope)
        {
            BytecodeChunk = bytecodeChunk;
            Scope = scope;
            _instrIdx = InitInstrIdx;
        }

        /// <summary>
        /// Marks this frame as a constructor call whose caller receives
        /// <paramref name="instance"/> in place of the frame's own return value.
        /// </summary>
        public void SetConstructionResult(SourceValue instance)
        {
            ConstructionResult = instance;
            HasConstructionResult = true;
        }

        /// <summary>Advances the instruction pointer by one.</summary>
        public void MoveToNextInstr()
        {
            _instrIdx++;
        }

        /// <summary>
        /// Sets the instruction pointer to <paramref name="instrIdx"/> and returns its previous
        /// value.
        /// </summary>
        public void JumpToInstr(int instrIdx)
        {
            _instrIdx = instrIdx;
        }
    }
}
