using Chow.SourceData;

namespace Chow
{
    /// <summary>
    /// Represents a Chow <c>list</c>, an ordered, mutable sequence of <see cref="ChowObject"/>s.
    /// Provides operations for reading, adding, and removing its items.
    /// </summary>
    public class ChowList : IChowObject
    {
        // Temporary solution

        internal ChowObject WrappedObject
        {
            get;
        }

        /// <summary>The number of items in the list.</summary>
        public int Length => WrappedObject.Length;

        /// <inheritdoc/>
        public bool IsNone => WrappedObject.IsNone;

        /// <inheritdoc/>
        public bool IsList => WrappedObject.IsList;

        /// <inheritdoc/>
        public bool IsDictionary => WrappedObject.IsDictionary;

        /// <inheritdoc/>
        public bool IsScope => WrappedObject.IsScope;

        /// <inheritdoc/>
        public bool IsString => WrappedObject.IsString;

        /// <inheritdoc/>
        public bool IsClass => WrappedObject.IsClass;

        /// <inheritdoc/>
        public bool IsClassInstance => WrappedObject.IsClassInstance;

        /// <summary>Gets or sets the item at the given index.</summary>
        /// <param name="index">The zero-based index of the item.</param>
        /// <returns>The item at <paramref name="index"/>.</returns>
        public ChowObject this[int index]
        {
            get => WrappedObject[index];
            set => WrappedObject[index] = value;
        }

        /// <summary>Creates a new, empty Chow list.</summary>
        public ChowList()
        {
            WrappedObject = (ChowObject)ChowObjectFactory.CreateList();
        }

        ChowList(ChowObject wrappedObject)
        {
            WrappedObject = wrappedObject;
        }

        /// <summary>Adds an object to the end of the list.</summary>
        /// <param name="object">The object to add.</param>
        public void Append(ChowObject @object)
        {
            WrappedObject.Call(SourceObjectConsts.ListAppendMethodName, @object);
        }

        /// <summary>
        /// Inserts an object at the given index, shifting later items toward the end.
        /// </summary>
        /// <param name="index">The index at which to insert the object.</param>
        /// <param name="object">The object to insert.</param>
        public void Insert(ChowObject index, ChowObject @object)
        {
            WrappedObject.Call(SourceObjectConsts.ListInsertMethodName, index, @object);
        }

        /// <summary>Removes and returns the item at the given index.</summary>
        /// <param name="index">The index of the item to remove.</param>
        /// <returns>The removed item.</returns>
        public ChowObject Pop(ChowObject index)
        {
            return WrappedObject.Call(SourceObjectConsts.ListPopMethodName, index);
        }

        /// <summary>Removes the first item equal to the given object.</summary>
        /// <param name="value">The object to remove.</param>
        public void Remove(ChowObject value)
        {
            WrappedObject.Call(SourceObjectConsts.ListRemoveMethodName, value);
        }

        /// <summary>Reverses the order of the list's items in place.</summary>
        /// <returns>The Chow <c>None</c> object.</returns>
        public ChowObject Reverse()
        {
            return WrappedObject.Call(SourceObjectConsts.ListReverseMethodName);
        }

        /// <summary>Removes all items from the list.</summary>
        public void Clear()
        {
            WrappedObject.Call(SourceObjectConsts.ListClearMethodName);
        }

        /// <summary>Converts a <see cref="ChowList"/> to a <see cref="ChowObject"/>.</summary>
        public static implicit operator ChowObject(ChowList value)
        {
            return value.WrappedObject;
        }

        /// <summary>Converts a <see cref="ChowObject"/> to a <see cref="ChowList"/>.</summary>
        public static implicit operator ChowList(ChowObject chowObj)
        {
            return new ChowList(chowObj);
        }

        /// <summary>Returns the Chow string representation of this list.</summary>
        /// <returns>The string representation of this list.</returns>
        public override string ToString()
        {
            return WrappedObject.ToString();
        }
    }
}
