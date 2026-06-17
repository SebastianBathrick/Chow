using Chow.SourceData;

namespace Chow
{
    /// <summary>
    /// Represents a Chow scope, a named collection of variable bindings. A scope can be supplied to
    /// <see cref="ChowEngine.Run"/> to carry variable state across separate executions.
    /// </summary>
    public class ChowScope : IChowObject
    {
        /// <summary>The number of variables defined in the scope.</summary>
        public int Length => WrappedObject.Length;

        /// <inheritdoc/>
        public bool IsNone => WrappedObject.IsNone;

        /// <inheritdoc/>
        public bool IsList => WrappedObject.IsList;

        /// <inheritdoc/>
        public bool IsDictionary => WrappedObject.IsDictionary;

        /// <inheritdoc/>
        public bool IsScope => WrappedObject.IsScope;

        internal ChowObject WrappedObject
        {
            get;
        }

        /// <summary>Gets or sets the value of the named variable, defining it if absent.</summary>
        /// <param name="key">The name of the variable to look up or assign.</param>
        /// <returns>The value of the named variable.</returns>
        public ChowObject this[ChowObject key]
        {
            get => WrappedObject[key];
            set => WrappedObject[key] = value;
        }

        /// <summary>Creates a new, empty Chow scope.</summary>
        public ChowScope()
        {
            WrappedObject = (ChowObject)ChowObjectFactory.CreateScope();
        }

        internal ChowScope(ChowObject wrappedObject)
        {
            WrappedObject = wrappedObject;
        }

        /// <summary>Converts a <see cref="ChowScope"/> to a <see cref="ChowObject"/>.</summary>
        public static implicit operator ChowObject(ChowScope scope)
        {
            return scope.WrappedObject;
        }

        /// <summary>Converts a <see cref="ChowObject"/> to a <see cref="ChowScope"/>.</summary>
        public static implicit operator ChowScope(ChowObject obj)
        {
            return new ChowScope(obj);
        }

        /// <summary>Returns the Chow string representation of this scope.</summary>
        /// <returns>The string representation of this scope.</returns>
        public override string ToString()
        {
            return WrappedObject.ToString();
        }
    }
}
