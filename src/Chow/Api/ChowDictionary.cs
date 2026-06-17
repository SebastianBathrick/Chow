using Chow.SourceData;

namespace Chow
{
    /// <summary>
    /// Represents a Chow <c>dict</c>, a mutable mapping of <see cref="ChowObject"/> keys to
    /// <see cref="ChowObject"/> values. Provides operations for reading, adding, and removing
    /// entries.
    /// </summary>
    public class ChowDictionary : IChowObject
    {
        internal ChowObject WrappedObject
        {
            get;
        }

        /// <summary>The number of entries in the dictionary.</summary>
        public int Length => WrappedObject.Length;

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
        public ChowDictionary()
        {
            WrappedObject = (ChowObject)ChowObjectFactory.CreateDictionary();
        }

        internal ChowDictionary(ChowObject wrappedObject)
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
            return WrappedObject.Call(SourceObjectConsts.DictionaryGetMethodName, key);
        }

        /// <summary>Returns the value associated with the given key, or a default if it is absent.</summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="defaultObject">The object to return if the key is absent.</param>
        /// <returns>The value associated with <paramref name="key"/>, or <paramref name="defaultObject"/>.</returns>
        public ChowObject Get(ChowObject key, ChowObject defaultObject)
        {
            return WrappedObject.Call(SourceObjectConsts.DictionaryGetMethodName, key, defaultObject);
        }

        /// <summary>Removes the entry with the given key and returns its value.</summary>
        /// <param name="key">The key of the entry to remove.</param>
        /// <returns>The value that was associated with <paramref name="key"/>.</returns>
        public ChowObject Pop(ChowObject key)
        {
            return WrappedObject.Call(SourceObjectConsts.DictionaryPopMethodName, key);
        }

        /// <summary>Removes the entry with the given key and returns its value, or a default if the key is absent.</summary>
        /// <param name="key">The key of the entry to remove.</param>
        /// <param name="defaultObject">The object to return if the key is absent.</param>
        /// <returns>The removed value, or <paramref name="defaultObject"/> if the key is absent.</returns>
        public ChowObject Pop(ChowObject key, ChowObject defaultObject)
        {
            return WrappedObject.Call(SourceObjectConsts.DictionaryPopMethodName, key, defaultObject);
        }

        /// <summary>Copies the entries from another dictionary, overwriting any existing keys.</summary>
        /// <param name="other">The dictionary whose entries are copied in.</param>
        public void Update(ChowObject other)
        {
            WrappedObject.Call(SourceObjectConsts.DictionaryUpdateMethodName, other);
        }

        /// <summary>
        /// Returns the value associated with the given key, inserting it with the Chow <c>None</c>
        /// object if it is absent.
        /// </summary>
        /// <param name="key">The key to look up or insert.</param>
        /// <returns>The existing value, or the Chow <c>None</c> object if the key was inserted.</returns>
        public ChowObject SetDefault(ChowObject key)
        {
            return WrappedObject.Call(SourceObjectConsts.DictionarySetMethodName, key);
        }

        /// <summary>
        /// Returns the value associated with the given key, inserting it with a default if it is
        /// absent.
        /// </summary>
        /// <param name="key">The key to look up or insert.</param>
        /// <param name="defaultObject">The object to insert if the key is absent.</param>
        /// <returns>The existing value, or <paramref name="defaultObject"/> if the key was inserted.</returns>
        public ChowObject SetDefault(ChowObject key, ChowObject defaultObject)
        {
            return WrappedObject.Call(SourceObjectConsts.DictionarySetMethodName, key, defaultObject);
        }

        /// <summary>Removes all entries from the dictionary.</summary>
        public void Clear()
        {
            WrappedObject.Call(SourceObjectConsts.DictionaryClearMethodName);
        }

        /// <summary>
        /// Converts a <see cref="ChowDictionary"/> to a <see cref="ChowObject"/>.
        /// </summary>
        public static implicit operator ChowObject(ChowDictionary value)
        {
            return value.WrappedObject;
        }

        /// <summary>C
        /// onverts a <see cref="ChowObject"/> to a <see cref="ChowDictionary"/>.
        /// </summary>
        public static implicit operator ChowDictionary(ChowObject @object)
        {
            return new ChowDictionary(@object);
        }

        /// <summary>Returns the Chow string representation of this dictionary.</summary>
        /// <returns>The string representation of this dictionary.</returns>
        public override string ToString()
        {
            return WrappedObject.ToString();
        }
    }
}
