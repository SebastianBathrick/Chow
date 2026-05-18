using Chow.Interpreter.Bytecode;
namespace Chow.Interpreter.Values.DataTypes
{
    /// <summary>
    /// Compile-time-only representation of a function. Stored as a constant in the parent chunk
    /// and consumed by the <c>PushNewClosureFromTemplate</c> op at runtime, which combines this template with
    /// the currently-active scope to produce a real <see cref="Closure"/>.
    /// </summary>
    sealed class ClosureTemplate
    {
        /// <summary>The compiled bytecode of the function body.</summary>
        public Chunk Chunk { get; }

        /// <summary>The function name as written in source.</summary>
        public string Name { get; }

        /// <summary>Declared positional-parameter count.</summary>
        public int ParamCount { get; }

        public ClosureTemplate(Chunk chunk, string name, int paramCount)
        {
            Chunk = chunk;
            Name = name;
            ParamCount = paramCount;
        }
    }
}
