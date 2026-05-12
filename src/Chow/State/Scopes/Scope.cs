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
    abstract class Scope : IScope
    {
        protected const string SCOPE_BOUNDARY_ELEMENT = "<SCOPE_BOUNDARY>";
        protected const int OUTERMOST_SCOPE_DEPTH = 0;

        readonly Stack<string> _varNames;
        readonly Dictionary<string, TaggedUnion> _varMap;
        int _scopeDepth;

        /// <summary>True when no nested block has been entered (scope depth is 0).</summary>
        public bool IsOutermostDepth => _scopeDepth == OUTERMOST_SCOPE_DEPTH;

        /// <inheritdoc/>
        public virtual IScope ParentOrNull => null;

        protected Scope()
        {
            _varMap = new Dictionary<string, TaggedUnion>();
            _scopeDepth = OUTERMOST_SCOPE_DEPTH;

            // The bottom of the stack represents the outermost scope (which will never be popped)
            _varNames = new Stack<string>();
            _varNames.Push(SCOPE_BOUNDARY_ELEMENT);
        }

        /// <inheritdoc/>
        public bool IsVariableDefined(string name)
        {
            return _varMap.ContainsKey(name);
        }

        /// <inheritdoc/>
        public void EnterNestedScope()
        {
            _scopeDepth++;
            _varNames.Push(SCOPE_BOUNDARY_ELEMENT);
        }

        /// <inheritdoc/>
        public void ExitNestedScope()
        {
            // Pop the name of the variable declared last OR the boundary element if no variables were declared in the current scope
            var popName = _varNames.Pop();

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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public TaggedUnion GetVariableValue(string name)
        {
            return _varMap[name];
        }
    }
}
