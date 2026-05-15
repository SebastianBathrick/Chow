namespace Chow.Interpreter.Values
{
    /// <summary>
    /// Wraps an arbitrary object that has no dedicated Chow value type. Typically returned when the interpreter
    /// holds a host-injected value that does not map to any of the standard Chow types.
    /// </summary>
    public class ChowDynamic : ChowValue
    {
        /// <summary>Gets the wrapped object.</summary>
        public object Value { get; }

        /// <summary>Initialises a new <see cref="ChowDynamic"/> wrapping <paramref name="val"/>.</summary>
        /// <param name="val">The object to wrap.</param>
        public ChowDynamic(object val)
        {
            Value = val;
        }

        /// <summary>
        /// Extracts the underlying value as <typeparamref name="TDataType"/> if <see cref="Value"/> is
        /// assignable to that type.
        /// </summary>
        /// <exception cref="InvalidCastException"><see cref="Value"/> is not assignable to <typeparamref name="TDataType"/>.</exception>
        public override TDataType AsType<TDataType>()
        {
            if (Value is TDataType value)
            {
                return value;
            }

            throw new InvalidCastException(GetType(), typeof(TDataType), this);
        }

        /// <summary>
        /// Returns <see langword="true"/> if <see cref="Value"/> is assignable to <typeparamref name="TDataType"/>.
        /// </summary>
        public override bool IsType<TDataType>()
        {
            return Value is TDataType;
        }

        /// <summary>
        /// Returns the string representation of the wrapped object, or an empty string if <see cref="Value"/>
        /// is <see langword="null"/>.
        /// </summary>
        public override string ToString()
        {
            return Value == null ? string.Empty : Value.ToString();
        }
    }
}
