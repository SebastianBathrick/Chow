using Chow.SourceData;
namespace Chow.Bytecode
{
    /// <summary>
    /// Compile-time-only representation of a function. Stored as a constant in the parent chunk
    /// and consumed by the <c>PushNewSourceFunction</c> op at runtime, which combines this template with
    /// the currently active scope to produce a real <see cref="SourceFunction"/>.
    /// </summary>
    sealed class FunctionDefinition
    {
        /// <summary>The compiled bytecode of the function body.</summary>
        public Chunk Chunk { get; }

        /// <summary>The function name as written in source.</summary>
        public string Name { get; }

        /// <summary>Declared positional-parameter count.</summary>
        public int ParamCount { get; }

        public FunctionDefinition(Chunk chunk, string name, int paramCount)
        {
            Chunk = chunk;
            Name = name;
            ParamCount = paramCount;
        }

        /// <summary>
        /// Combines this template with the scope active at <c>def</c> time to produce the
        /// runtime closure value.
        /// </summary>
        public ISourceObject MakeClosure(Scope enclosing)
        {
            return new SourceFunction(Chunk, enclosing, Name, ParamCount);
        }
    }
}
