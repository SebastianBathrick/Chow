namespace Chow.Interpreter.Values
{
    /// <summary>
    /// Represents a Chow callable (a function or closure). Returned when a global variable holds a function
    /// value. Instances can be passed back to the host scope via the <see cref="ChowModule"/> indexer setter
    /// or <see cref="ChowModule.SetGlobal"/> to store a function under a different name.
    /// </summary>
    public class ChowFunction : ChowValue
    {
        /// <summary>Gets the underlying callable object.</summary>
        public object Value { get; }

        /// <summary>Initialises a new <see cref="ChowFunction"/> wrapping <paramref name="value"/>.</summary>
        /// <param name="value">The callable to wrap.</param>
        public ChowFunction(object value)
        {
            Value = value;
        }

        /// <inheritdoc/>
        /// <exception cref="InvalidCastException">Always thrown — functions have no supported conversions.</exception>
        public override TDataType AsType<TDataType>()
        {
            throw new InvalidCastException(GetType(), typeof(TDataType), this);
        }

        /// <summary>Always returns <see langword="false"/>.</summary>
        public override bool IsType<TDataType>()
        {
            return false;
        }

        /// <summary>
        /// Returns the string representation of the underlying callable, or an empty string if
        /// <see cref="Value"/> is <see langword="null"/>.
        /// </summary>
        public override string ToString()
        {
            return Value == null ? string.Empty : Value.ToString();
        }
    }
}
