using System.Collections.Generic;
namespace Chow.Interpreter.State
{
    /// <summary>
    /// A flat variable-binding store linked to an optional parent for LEGB chain walking. Used for both module-level
    /// scope (no parent) and per-function-call scope (parent = the closure's captured enclosing scope). Block bodies
    /// share their enclosing scope — Chow matches Python's function-level scoping.
    /// </summary>
    sealed class Scope
    {
        readonly Dictionary<string, ChowValue> _varMap;

        /// <summary>
        /// The enclosing scope used for LEGB chain walking, or <c>null</c> at the top of the chain.
        /// If there is no parent then the property will be <see langword="null"/>.
        /// </summary>
        public Scope Parent { get; }

        /// <summary>
        /// Creates a scope with the given parent (pass <c>null</c> for the module-level scope).
        /// </summary>
        public Scope(Scope parent = null)
        {
            Parent = parent;
            _varMap = new Dictionary<string, ChowValue>();
        }

        /// <summary>
        /// True if <paramref name="name"/> is bound in this scope. Does not consult
        /// <see cref="Parent"/>.
        /// </summary>
        public bool ContainsVariable(string name)
        {
            return _varMap.ContainsKey(name);
        }

        /// <summary>
        /// Binds <paramref name="name"/> to <paramref name="value"/> in this scope. Creates the
        /// binding if it does not exist; otherwise overwrites it in place.
        /// </summary>
        public void AssignVariableValue(string name, ChowValue value)
        {
            _varMap[name] = value;
        }

        /// <summary>
        /// Returns the value bound to <paramref name="name"/> in this scope. Throws if undefined.
        /// </summary>
        public ChowValue GetVariableValue(string name)
        {
            return _varMap[name];
        }

        /// <summary>
        /// Removes the binding for <paramref name="name"/> from this scope. Returns <c>true</c> if
        /// a binding was removed, <c>false</c> if no such binding existed. Does not consult
        /// <see cref="Parent"/>.
        /// </summary>
        public bool TryRemoveVariable(string name)
        {
            return _varMap.Remove(name);
        }

        /// <summary>
        /// Creates a new Scope where <paramref name="right"/> variables are combined with
        /// <paramref name="left"/> variables, and any variables in the left scope that share a
        /// name with one in the right will use the right's value. The left operand's parent will
        /// always be the new scope's parent (assuming the left has a parent).
        /// </summary>
        /// <param name="left">The scope that will have its variables added to the new scope first.</param>
        /// <param name="right">The scope that will have its variables added to the new scope second.</param>
        /// <returns>Scope containing variables found both in <paramref name="left"/> and
        /// <paramref name="right"/>.</returns>
        public static Scope operator +(in Scope left, in Scope right)
        {
            var newScope = new Scope(left.Parent);

            foreach (var nameValPair in left._varMap)
            {
                newScope.AssignVariableValue(nameValPair.Key, nameValPair.Value);
            }

            foreach (var nameValPair in right._varMap)
            {
                newScope.AssignVariableValue(nameValPair.Key, nameValPair.Value);
            }

            return newScope;
        }
    }
}
