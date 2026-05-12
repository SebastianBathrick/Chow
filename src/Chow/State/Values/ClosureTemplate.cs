using Chow.Interpreter.Bytecode;

namespace Chow.Interpreter.State.Values
{
    /// <summary>
    /// Compile-time-only representation of a function. Stored as a constant in the parent chunk
    /// and consumed by the <c>MakeClosure</c> op at runtime, which combines this template with
    /// the currently-active scope to produce a real <see cref="Closure"/>.
    /// </summary>
    internal sealed class ClosureTemplate
    {
        readonly Chunk _chunk;
        readonly string _name;
        readonly int _paramCount;

        /// <summary>The compiled bytecode of the function body.</summary>
        public Chunk Chunk => _chunk;

        /// <summary>The function name as written in source.</summary>
        public string Name => _name;

        /// <summary>Declared positional-parameter count.</summary>
        public int ParamCount => _paramCount;

        public ClosureTemplate(Chunk chunk, string name, int paramCount)
        {
            _chunk = chunk;
            _name = name;
            _paramCount = paramCount;
        }
    }
}
