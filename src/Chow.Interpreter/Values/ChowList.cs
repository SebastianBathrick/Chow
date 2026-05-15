using Chow.Interpreter.State.Values;

namespace Chow.Interpreter.Values
{
    /// <summary>Represents a Chow list value.</summary>
    public class ChowList : ChowValue
    {
        internal InternalList Internal { get; }

        /// <summary>Gets the number of elements in the list.</summary>
        public int Count => Internal.Count;

        /// <summary>Gets the element at the specified zero-based index as a <see cref="ChowValue"/>.</summary>
        /// <param name="index">The zero-based index.</param>
        public ChowValue this[int index] => ApiConverter.ToChowValue(Internal[index]);

        /// <summary>Initialises a new empty <see cref="ChowList"/>.</summary>
        public ChowList()
        {
            Internal = new InternalList();
        }

        /// <summary>Initialises a new <see cref="ChowList"/> as a shallow copy of <paramref name="source"/>.</summary>
        /// <param name="source">The list to copy.</param>
        public ChowList(ChowList source)
        {
            Internal = InternalList.Concat(source.Internal, new InternalList());
        }

        internal ChowList(InternalList wrapped)
        {
            Internal = wrapped;
        }

        /// <summary>
        /// Extracts the underlying value as <typeparamref name="TDataType"/>. Supported conversions:
        /// <see langword="bool"/> (<see langword="true"/> when non-empty).
        /// </summary>
        /// <exception cref="InvalidCastException"><typeparamref name="TDataType"/> is not a supported conversion.</exception>
        public override TDataType AsType<TDataType>()
        {
            if (typeof(TDataType) == typeof(bool))
            {
                return (TDataType)(object)(Internal.Count != 0);
            }

            throw new InvalidCastException(GetType(), typeof(TDataType), this);
        }

        /// <summary>Always returns <see langword="false"/>.</summary>
        public override bool IsType<TDataType>()
        {
            return false;
        }

        /// <summary>Returns the string representation of the list.</summary>
        public override string ToString() => Internal.ToString();
    }
}
