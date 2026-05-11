namespace Chow.Interpreter.Evaluation
{
    /// <summary>
    /// Per-function-call scope. Constructed fresh each time a <see cref="Closure"/> is invoked
    /// and discarded when the call returns (unless captured by a nested closure). Represents the
    /// L (Local) layer of LEGB; <see cref="ParentOrNull"/> exposes the captured enclosing scope
    /// (E), chaining upward through any outer locals to the terminating <see cref="ModuleScope"/>.
    /// </summary>
    internal sealed class LocalScope : Scope
    {
        readonly Scope _parent;

        /// <summary>The enclosing scope captured when this local was created. Never <c>null</c> for a function call.</summary>
        public override Scope ParentOrNull => _parent;

        /// <summary>Creates an empty local scope chained to <paramref name="parent"/>.</summary>
        /// <param name="parent">The enclosing scope (another <see cref="LocalScope"/> for nested defs, or the <see cref="ModuleScope"/> for top-level defs).</param>
        public LocalScope(Scope parent) : base()
        {
            _parent = parent;
        }
    }
}
