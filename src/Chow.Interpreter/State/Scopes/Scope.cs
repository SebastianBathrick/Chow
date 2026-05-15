using Chow.Interpreter.State.Values;
using System.Collections.Generic;

namespace Chow.Interpreter.State.Scopes
{
    /// <summary>
    /// A flat variable-binding store linked to an optional parent for LEGB chain walking. Used for both module-level
    /// scope (no parent) and per-function-call scope (parent = the closure's captured enclosing scope). Block bodies
    /// share their enclosing scope — Chow matches Python's function-level scoping.
    /// </summary>
    sealed class Scope
    {
        readonly Dictionary<string, TaggedUnion> _varMap;

        /// <summary>The enclosing scope used for LEGB chain walking, or <c>null</c> at the top of the chain.</summary>
        public Scope ParentOrNull { get; }

        /// <summary>Creates a scope with the given parent (pass <c>null</c> for the module-level scope).</summary>
        public Scope(Scope parentOrNull = null)
        {
            ParentOrNull = parentOrNull;
            _varMap = new Dictionary<string, TaggedUnion>();
        }

        /// <summary>True if <paramref name="name"/> is bound in this scope. Does not consult <see cref="ParentOrNull"/>.</summary>
        public bool IsVariableDefined(string name)
        {
            return _varMap.ContainsKey(name);
        }

        /// <summary>
        /// Binds <paramref name="name"/> to <paramref name="value"/> in this scope. Creates the binding
        /// if it does not exist; otherwise overwrites it in place.
        /// </summary>
        public void AssignVariableValue(string name, TaggedUnion value)
        {
            _varMap[name] = value;
        }

        /// <summary>Returns the value bound to <paramref name="name"/> in this scope. Throws if undefined.</summary>
        public TaggedUnion GetVariableValue(string name)
        {
            return _varMap[name];
        }
    }
}
