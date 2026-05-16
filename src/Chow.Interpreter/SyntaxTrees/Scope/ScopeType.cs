namespace Chow.Interpreter.SyntaxTrees.Scope
{
    /// <summary>
    /// How a name binding or reference resolves at runtime. Stamped onto name-bearing AST nodes by
    /// <see cref="SemanticAnalyzer"/> so the <see cref="Chow.Interpreter.Compiler"/>
    /// can emit the correct opcode without performing scope analysis itself.
    /// </summary>
    enum ScopeType
    {
        /// <summary>
        /// Default. Binding lands in the current frame's scope; reads walk the LEGB chain.
        /// Applies to module-level names and to any function-local name not declared
        /// <c>global</c> or <c>nonlocal</c>.
        /// </summary>
        Local,

        /// <summary>Binding/reference targets the module scope directly (declared <c>global</c>).</summary>
        Global,

        /// <summary>
        /// Binding/reference targets the nearest enclosing function scope that already binds the
        /// name (declared <c>nonlocal</c>). The module scope is excluded from the walk.
        /// </summary>
        Nonlocal
    }
}
