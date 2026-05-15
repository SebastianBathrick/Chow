using Chow.Interpreter.State.Values;

namespace Chow.Interpreter.Values
{
    /// <summary>Represents a Chow dictionary value.</summary>
    public class ChowDict : ChowValue
    {
        internal InternalDict Internal { get; }

        /// <summary>Gets the number of key-value pairs in the dictionary.</summary>
        public int Count => Internal.Count;

        /// <summary>Gets the value associated with <paramref name="key"/> as a <see cref="ChowValue"/>.</summary>
        /// <param name="key">The key to look up.</param>
        public ChowValue this[ChowValue key]
            => ApiConverter.ToChowValue(Internal[ApiConverter.ToTaggedUnion(key)]);

        /// <summary>Initialises a new empty <see cref="ChowDict"/>.</summary>
        public ChowDict()
        {
            Internal = new InternalDict();
        }

        /// <summary>Initialises a new <see cref="ChowDict"/> as a shallow copy of <paramref name="source"/>.</summary>
        /// <param name="source">The dictionary to copy.</param>
        public ChowDict(ChowDict source)
        {
            Internal = InternalDict.Merge(source.Internal, new InternalDict());
        }

        internal ChowDict(InternalDict wrapped)
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

        /// <summary>Returns the string representation of the dictionary.</summary>
        public override string ToString() => Internal.ToString();
    }
}
