using Chow.SourceData;

namespace Chow.Bytecode
{
    /// <summary>
    /// Compile-time-only representation of a function. Stored as a constant in the parent bytecodeChunk
    /// and consumed by the <c>PushNewSourceFunction</c> op at runtime, which combines this template with
    /// the currently active scope to produce a real <see cref="SourceFunction"/>.
    /// </summary>
    sealed class FunctionDefinition
    {
        /// <summary>The compiled bytecode of the function body.</summary>
        public BytecodeChunk BytecodeChunk { get; }

        /// <summary>The function name as written in source.</summary>
        public string Name { get; }

        /// <summary>Declared positional-parameter count.</summary>
        public int ParamCount { get; }

        public FunctionDefinition(BytecodeChunk bytecodeChunk, string name, int paramCount)
        {
            BytecodeChunk = bytecodeChunk;
            Name = name;
            ParamCount = paramCount;
        }

        /// <summary>
        /// Combines this template with the scope active at <c>def</c> time to produce the
        /// runtime closure value.
        /// </summary>
        public ISourceObject MakeClosure(Scope enclosing)
        {
            return new SourceFunction(BytecodeChunk, enclosing, Name, ParamCount);
        }
    }
}
