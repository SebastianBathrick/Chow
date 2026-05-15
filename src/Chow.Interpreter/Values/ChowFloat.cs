namespace Chow.Interpreter.Values
{
    /// <summary>Represents a Chow float value, backed by a 64-bit double-precision floating-point number.</summary>
    public class ChowFloat : ChowValue
    {
        readonly double _val;

        /// <summary>Initialises a new <see cref="ChowFloat"/> with the given value.</summary>
        /// <param name="val">The floating-point value.</param>
        public ChowFloat(double val)
        {
            _val = val;
        }

        /// <summary>
        /// Extracts the underlying value as <typeparamref name="TDataType"/>. Supported conversions:
        /// <see langword="double"/> (identity), <see langword="long"/> (truncated),
        /// <see langword="bool"/> (<see langword="true"/> when non-zero).
        /// </summary>
        /// <exception cref="InvalidCastException"><typeparamref name="TDataType"/> is not a supported conversion.</exception>
        public override TDataType AsType<TDataType>()
        {
            if (typeof(TDataType) == typeof(double))
            {
                return (TDataType)(object)_val;
            }

            if (typeof(TDataType) == typeof(long))
            {
                return (TDataType)(object)(long)_val;
            }

            if (typeof(TDataType) == typeof(bool))
            {
                return (TDataType)(object)(_val != 0.0);
            }

            throw new InvalidCastException(GetType(), typeof(TDataType), this);
        }

        /// <summary>Returns <see langword="true"/> only when <typeparamref name="TDataType"/> is <see langword="double"/>.</summary>
        public override bool IsType<TDataType>()
        {
            return typeof(TDataType) == typeof(double);
        }

        /// <summary>Returns the string representation of the float value.</summary>
        public override string ToString() => _val.ToString();
    }
}
