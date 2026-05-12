using Chow.Interpreter.State.Values;
using System.Collections.Generic;

namespace Chow.Interpreter.State.Scopes
{
    /// <summary>
    /// Base class for all runtime variable scopes. Stores bindings in a flat dictionary backed by a
    /// name stack with boundary sentinels, which together support nested-block enter/exit within a
    /// single scope. Subclasses (<see cref="ModuleScope"/>, <see cref="LocalScope"/>) differentiate
    /// the role of the scope in the LEGB lookup chain.
    /// </summary>
    /// <remarks>
    /// No source identifier can start with <c>&lt;</c>, so <c>SCOPE_BOUNDARY_ELEMENT</c> never
    /// collides with a real variable name.
    /// </remarks>
    abstract class Scope
    {
        protected const string SCOPE_BOUNDARY_ELEMENT = "<SCOPE_BOUNDARY>";
        protected const int OUTERMOST_SCOPE_DEPTH = 0;

        Stack<string> _varNames;
        Dictionary<string, TaggedUnion> _varMap;
        int _scopeDepth;

        /// <summary>True when no nested block has been entered (scope depth is 0).</summary>
        public bool IsOutermostDepth => _scopeDepth == OUTERMOST_SCOPE_DEPTH;

        /// <summary>
        /// The enclosing scope used for LEGB chain walking, or <c>null</c> at the top of the chain.
        /// Returns <c>null</c> by default; overridden by <see cref="LocalScope"/>.
        /// </summary>
        public virtual Scope ParentOrNull => null;

        protected Scope()
        {
            _varMap = new Dictionary<string, TaggedUnion>();
            _scopeDepth = OUTERMOST_SCOPE_DEPTH;

            // The bottom of the stack represents the outermost scope (which will never be popped)
            _varNames = new Stack<string>();
            _varNames.Push(SCOPE_BOUNDARY_ELEMENT);
        }

        /// <summary>True if <paramref name="name"/> is bound in this scope. Does not consult <see cref="ParentOrNull"/>.</summary>
        public bool IsVariableDefined(string name)
        {
            return _varMap.ContainsKey(name);
        }

        /// <summary>Begins a new nested block. Subsequent assignments are tracked for removal on the matching <see cref="ExitNestedScope"/>.</summary>
        public void EnterNestedScope()
        {
            _scopeDepth++;
            _varNames.Push(SCOPE_BOUNDARY_ELEMENT);
        }

        /// <summary>
        /// Ends the innermost nested block, removing every binding first declared inside it.
        /// Rebindings of outer names made within the block are left in place (Python block semantics).
        /// </summary>
        public void ExitNestedScope()
        {
            // Pop the name of the variable declared last OR the boundary element if no variables were declared in the current scope
            string popName = _varNames.Pop();

            // Pop until the boundary element has been popped (either popped or is below the name of the first variable in the scope)
            while (popName != SCOPE_BOUNDARY_ELEMENT)
            {
                // Remove variable name and its assigned value from the map
                _varMap.Remove(popName);

                // Pop another variable name OR the scope boundary element if there's no more variables left in the scope
                popName = _varNames.Pop();
            }

            _scopeDepth--;
        }

        /// <summary>
        /// Binds <paramref name="name"/> to <paramref name="value"/> in this scope. Creates the binding
        /// if it does not exist; otherwise overwrites it in place.
        /// </summary>
        public void AssignVariableValue(string name, TaggedUnion value)
        {
            // First-time assignment also declares: track the name in the current scope
            // so it gets removed from the value map when the scope exits.
            if (!_varMap.ContainsKey(name))
            {
                _varNames.Push(name);
            }

            _varMap[name] = value;
        }

        /// <summary>Returns the value bound to <paramref name="name"/> in this scope. Throws if undefined.</summary>
        public TaggedUnion GetVariableValue(string name)
        {
            return _varMap[name];
        }
    }
}
