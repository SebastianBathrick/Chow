using Chow.Interpreter.State.Values;

namespace Chow.Interpreter.State.Scopes
{
    internal interface IScope
    {
        /// <summary>True when no nested block has been entered (scope depth is 0).</summary>
        bool IsOutermostDepth { get; }

        /// <summary>
        /// The enclosing scope used for LEGB chain walking, or <c>null</c> at the top of the chain.
        /// Returns <c>null</c> by default; overridden by <see cref="LocalScope"/>.
        /// </summary>
        IScope ParentOrNull { get; }

        /// <summary>True if <paramref name="name"/> is bound in this scope. Does not consult <see cref="ParentOrNull"/>.</summary>
        bool IsVariableDefined(string name);

        /// <summary>Begins a new nested block. Subsequent assignments are tracked for removal on the matching <see cref="ExitNestedScope"/>.</summary>
        void EnterNestedScope();

        /// <summary>
        /// Ends the innermost nested block, removing every binding first declared inside it.
        /// Rebinding of outer names made within the block are left in place (Python block semantics).
        /// </summary>
        void ExitNestedScope();

        /// <summary>
        /// Binds <paramref name="name"/> to <paramref name="value"/> in this scope. Creates the binding
        /// if it does not exist; otherwise overwrites it in place.
        /// </summary>
        void AssignVariableValue(string name, TaggedUnion value);

        /// <summary>Returns the value bound to <paramref name="name"/> in this scope. Throws if undefined.</summary>
        TaggedUnion GetVariableValue(string name);
        }
    }

