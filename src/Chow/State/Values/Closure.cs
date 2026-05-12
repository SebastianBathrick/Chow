using Chow.Interpreter.Bytecode;
using Chow.Interpreter.State.Scopes;

namespace Chow.Interpreter.State.Values
{
    /// <summary>
    /// Runtime function value produced by a <c>def</c> statement in Chow source (as opposed to
    /// interop delegates supplied by the host language). Pairs a compiled <see cref="Chunk"/>
    /// with the scope active at the moment <c>def</c> ran, so the function body can later
    /// resolve enclosing names via the LEGB chain.
    /// </summary>
    /// <remarks>
    /// <see cref="Enclosing"/> is a live reference — never a copy. Mutations to that scope
    /// after capture remain visible to the function body, matching Python closure semantics.
    /// </remarks>
    internal sealed class Closure
    {
        readonly Chunk _chunk;
        readonly Scope _enclosing;
        readonly string _name;
        readonly int _paramCount;

        /// <summary>The compiled bytecode of the function body.</summary>
        public Chunk Chunk => _chunk;

        /// <summary>The scope active when <c>def</c> executed; used as the parent of the call's local scope.</summary>
        public Scope Enclosing => _enclosing;

        /// <summary>The function name as written in source. Used for diagnostics and stack traces.</summary>
        public string Name => _name;

        /// <summary>Declared positional-parameter count. Used by the VM for arity checking at call sites.</summary>
        public int ParamCount => _paramCount;

        /// <summary>Constructs a closure. All fields are readonly; closures are immutable once built.</summary>
        public Closure(Chunk chunk, Scope enclosing, string name, int paramCount)
        {
            _chunk = chunk;
            _enclosing = enclosing;
            _name = name;
            _paramCount = paramCount;
        }
    }
}
