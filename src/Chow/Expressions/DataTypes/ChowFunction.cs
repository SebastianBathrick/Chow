using Chow.Bytecode;
namespace Chow.DataTypes
{
    /// <summary>
    /// Compile-time-only representation of a function. Stored as a constant in the parent chunk
    /// and consumed by the <c>PushNewClosureFromTemplate</c> op at runtime, which combines this template with
    /// the currently active scope to produce a real <see cref="ChowFunctionInstance"/>.
    /// </summary>
    sealed class ChowFunction
    {
        /// <summary>The compiled bytecode of the function body.</summary>
        public Chunk Chunk { get; }

        /// <summary>The function name as written in source.</summary>
        public string Name { get; }

        /// <summary>Declared positional-parameter count.</summary>
        public int ParamCount { get; }

        public ChowFunction(Chunk chunk, string name, int paramCount)
        {
            Chunk = chunk;
            Name = name;
            ParamCount = paramCount;
        }
    }
}
