using Chow.SourceData;

namespace Chow
{
    /// <summary>
    /// Represents a Chow <c>dict</c>, a mutable mapping of <see cref="ChowObject"/> keys to
    /// <see cref="ChowObject"/> values. Provides operations for reading, adding, and removing
    /// entries.
    /// </summary>
    public class ChowDict : IChowObject
    {
        internal ChowObject WrappedObject
        {
            get;
        }

        /// <summary>The number of entries in the dictionary.</summary>
        public int Length => WrappedObject.Length;

        /// <inheritdoc/>
        public bool IsNone => WrappedObject.IsNone;

        /// <inheritdoc/>
        public bool IsList => WrappedObject.IsList;

        /// <inheritdoc/>
        public bool IsDictionary => WrappedObject.IsDictionary;

        /// <inheritdoc/>
        public bool IsScope => WrappedObject.IsScope;

        /// <summary>
        /// Gets or sets the value associated with the given key, adding it if absent.
        /// </summary>
        /// <param name="key">The key to look up or assign.</param>
        /// <returns>The value associated with <paramref name="key"/>.</returns>
        public ChowObject this[ChowObject key]
        {
            get => WrappedObject[key];
            set => WrappedObject[key] = value;
        }

        /// <summary>Creates a new, empty Chow dictionary.</summary>
        public ChowDict()
        {
            WrappedObject = (ChowObject)ChowObjectFactory.CreateDict();
        }

        internal ChowDict(ChowObject wrappedObject)
        {
            WrappedObject = wrappedObject;
        }

        /// <summary>
        /// Returns the value associated with the given key, or the Chow <c>None</c> object if it is
        /// absent.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <returns>The value associated with <paramref name="key"/>, or the Chow <c>None</c>
        /// object.</returns>
        public ChowObject Get(ChowObject key)
        {
            return WrappedObject.Call(SourceObjectConsts.DictGetMethodName, key);
        }

        /// <summary>Removes the entry with the given key and returns its value.</summary>
        /// <param name="key">The key of the entry to remove.</param>
        /// <returns>The value that was associated with <paramref name="key"/>.</returns>
        public ChowObject Pop(ChowObject key)
        {
            return WrappedObject.Call(SourceObjectConsts.DictPopMethodName, key);
        }

        /// <summary>Copies the entries from another dictionary, overwriting any existing keys.</summary>
        /// <param name="other">The dictionary whose entries are copied in.</param>
        public void Update(ChowObject other)
        {
            WrappedObject.Call(SourceObjectConsts.DictUpdateMethodName, other);
        }

        /// <summary>Removes all entries from the dictionary.</summary>
        public void Clear()
        {
            WrappedObject.Call(SourceObjectConsts.DictClearMethodName);
        }

        /// <summary>
        /// Converts a <see cref="ChowDict"/> to a <see cref="ChowObject"/>.
        /// </summary>
        public static implicit operator ChowObject(ChowDict value)
        {
            return value.WrappedObject;
        }

        /// <summary>
        /// Converts a <see cref="ChowObject"/> to a <see cref="ChowDict"/>.
        /// </summary>
        public static implicit operator ChowDict(ChowObject @object)
        {
            return new ChowDict(@object);
        }

        /// <summary>Returns the Chow string representation of this dictionary.</summary>
        /// <returns>The string representation of this dictionary.</returns>
        public override string ToString()
        {
            return WrappedObject.ToString();
        }
    }
}
