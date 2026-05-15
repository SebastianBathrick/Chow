namespace Chow.Interpreter.Values
{
    /// <summary>Represents a Chow string value.</summary>
    public class ChowStr : ChowValue
    {
        /// <summary>Gets the underlying string value.</summary>
        public string Value { get; }

        /// <summary>Initialises a new <see cref="ChowStr"/> with the given value.</summary>
        /// <param name="val">The string value.</param>
        public ChowStr(string val)
        {
            Value = val;
        }

        /// <summary>
        /// Extracts the underlying value as <typeparamref name="TDataType"/>. Supported conversions:
        /// <see langword="bool"/> (<see langword="true"/> when non-empty). Use <see cref="Value"/> to
        /// obtain the string directly.
        /// </summary>
        /// <exception cref="InvalidCastException"><typeparamref name="TDataType"/> is not a supported conversion.</exception>
        public override TDataType AsType<TDataType>()
        {
            if (typeof(TDataType) == typeof(bool))
            {
                return (TDataType)(object)(Value.Length != 0);
            }

            throw new InvalidCastException(GetType(), typeof(TDataType), this);
        }

        /// <summary>Always returns <see langword="false"/>.</summary>
        public override bool IsType<TDataType>()
        {
            return false;
        }

        /// <summary>Returns the string value.</summary>
        public override string ToString() => Value;
    }
}
