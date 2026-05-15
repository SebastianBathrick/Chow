namespace Chow.Interpreter.Values
{
    /// <summary>Represents a Chow integer value, backed by a 64-bit signed integer.</summary>
    public class ChowInt : ChowValue
    {
        readonly long _val;

        /// <summary>Initialises a new <see cref="ChowInt"/> with the given value.</summary>
        /// <param name="val">The integer value.</param>
        public ChowInt(long val)
        {
            _val = val;
        }

        /// <summary>
        /// Extracts the underlying value as <typeparamref name="TDataType"/>. Supported conversions:
        /// <see langword="long"/> (identity), <see langword="double"/> (widening),
        /// <see langword="bool"/> (<see langword="true"/> when non-zero).
        /// </summary>
        /// <exception cref="InvalidCastException"><typeparamref name="TDataType"/> is not a supported conversion.</exception>
        public override TDataType AsType<TDataType>()
        {
            if (typeof(TDataType) == typeof(long))
            {
                return (TDataType)(object)_val;
            }

            if (typeof(TDataType) == typeof(double))
            {
                return (TDataType)(object)(double)_val;
            }

            if (typeof(TDataType) == typeof(bool))
            {
                return (TDataType)(object)(_val != 0);
            }

            throw new InvalidCastException(GetType(), typeof(TDataType), this);
        }

        /// <summary>Returns <see langword="true"/> only when <typeparamref name="TDataType"/> is <see langword="long"/>.</summary>
        public override bool IsType<TDataType>()
        {
            return typeof(TDataType) == typeof(long);
        }

        /// <summary>Returns the string representation of the integer value.</summary>
        public override string ToString() => _val.ToString();
    }
}
