using Chow.Interpreter.Compilation;
using Chow.Interpreter.Values;
using System.Collections.Generic;

namespace Chow.Interpreter.Evaluation
{
    internal class StackFrame
    {
        const int INIT_INSTR_IDX = 0;

        Chunk _chunk;
        LocalScope _scope;

        // TODO: Start using "instr" to abbreviate "instruction"
        int _instrIdx;

        public Chunk Chunk => _chunk;

        public LocalScope Scope => _scope;

        public Instruction CurrentInstr => _chunk[_instrIdx];

        public bool IsInstrToRun => _chunk.Count != _instrIdx;

        public StackFrame(Chunk chunk, LocalScope scope = null)
        {
            _chunk = chunk;
            _scope = scope == null ? new LocalScope() : scope;
            _instrIdx = INIT_INSTR_IDX;
        }

        public void MoveToNextInstr()
        {
            _instrIdx++;
        }

        public int JumpToInstr(int instrIdx)
        {
            int prevInstrIdx = _instrIdx;
            _instrIdx = instrIdx;
            return prevInstrIdx;
        }
    }
}
