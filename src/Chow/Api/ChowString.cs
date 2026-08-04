namespace Chow
{
    /// <summary>
    /// Represents a Chow <c>str</c>, an immutable sequence of characters. Provides read-only access
    /// to its characters and a set of host-side string operations.
    /// </summary>
    public class ChowString : IChowObject
    {
        internal ChowObject WrappedObject
        {
            get;
        }

        /// <summary>The number of characters in the string.</summary>
        public int Length => WrappedObject.ToString().Length;

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

        /// <summary>Gets the single-character string at the given index.</summary>
        /// <param name="index">The zero-based index of the character.</param>
        /// <returns>The character at <paramref name="index"/> as a Chow <c>str</c>.</returns>
        public ChowObject this[int index] => WrappedObject.ToString()[index].ToString();

        internal ChowString(string value)
        {
            WrappedObject = (ChowObject)ChowObjectFactory.CreateString(value);
        }

        /// <summary>Determines whether the string contains the given substring.</summary>
        /// <param name="value">The substring to search for.</param>
        /// <returns><c>true</c> if the string contains <paramref name="value"/>; otherwise,
        /// <c>false</c>.</returns>
        public bool Contains(string value)
        {
            return WrappedObject.ToString().Contains(value);
        }

        /// <summary>Determines whether the string starts with the given prefix.</summary>
        /// <param name="value">The prefix to test for.</param>
        /// <returns><c>true</c> if the string starts with <paramref name="value"/>; otherwise,
        /// <c>false</c>.</returns>
        public bool StartsWith(string value)
        {
            return WrappedObject.ToString().StartsWith(value);
        }

        /// <summary>Determines whether the string ends with the given suffix.</summary>
        /// <param name="value">The suffix to test for.</param>
        /// <returns><c>true</c> if the string ends with <paramref name="value"/>; otherwise,
        /// <c>false</c>.</returns>
        public bool EndsWith(string value)
        {
            return WrappedObject.ToString().EndsWith(value);
        }

        /// <summary>Returns the index of the first occurrence of the given substring.</summary>
        /// <param name="value">The substring to locate.</param>
        /// <returns>The zero-based index of <paramref name="value"/>, or -1 if it is not
        /// found.</returns>
        public int IndexOf(string value)
        {
            return WrappedObject.ToString().IndexOf(value, System.StringComparison.Ordinal);
        }

        /// <summary>Returns a substring beginning at the given index with the given length.</summary>
        /// <param name="startIndex">The zero-based starting index of the substring.</param>
        /// <param name="length">The number of characters in the substring.</param>
        /// <returns>The requested substring as a <see cref="ChowString"/>.</returns>
        public ChowString Substring(int startIndex, int length)
        {
            return new ChowString(WrappedObject.ToString().Substring(startIndex, length));
        }

        /// <summary>Returns a copy of the string converted to uppercase.</summary>
        /// <returns>The uppercase string as a <see cref="ChowString"/>.</returns>
        public ChowString ToUpper()
        {
            return new ChowString(WrappedObject.ToString().ToUpperInvariant());
        }

        /// <summary>Returns a copy of the string converted to lowercase.</summary>
        /// <returns>The lowercase string as a <see cref="ChowString"/>.</returns>
        public ChowString ToLower()
        {
            return new ChowString(WrappedObject.ToString().ToLowerInvariant());
        }

        /// <summary>Converts a <see cref="ChowString"/> to a <see cref="ChowObject"/>.</summary>
        public static implicit operator ChowObject(ChowString value)
        {
            return value.WrappedObject;
        }

        /// <summary>Converts a <see cref="ChowObject"/> to a <see cref="ChowString"/>.</summary>
        public static implicit operator ChowString(ChowObject @object)
        {
            return new ChowString(@object.ToString());
        }

        /// <summary>Converts a host <see cref="string"/> to a <see cref="ChowString"/>.</summary>
        public static implicit operator ChowString(string value)
        {
            return new ChowString(value);
        }

        /// <summary>Converts a <see cref="ChowString"/> to a host <see cref="string"/>.</summary>
        public static implicit operator string(ChowString value)
        {
            return value.WrappedObject.ToString();
        }

        /// <summary>Returns the Chow string representation of this string.</summary>
        /// <returns>The string representation of this string.</returns>
        public override string ToString()
        {
            return WrappedObject.ToString();
        }
    }
}
