namespace Chow.Interpreter.Values
{
    /// <summary>Represents a Chow boolean value.</summary>
    public class ChowBool : ChowValue
    {
        // TODO: Create a single source of truth for these type names
        const string TRUE_STRING = "True";
        const string FALSE_STRING = "False";

        readonly bool _val;

        /// <summary>Initialises a new <see cref="ChowBool"/> with the given value.</summary>
        /// <param name="val">The boolean value.</param>
        public ChowBool(bool val)
        {
            _val = val;
        }

        /// <summary>
        /// Extracts the underlying value as <typeparamref name="TDataType"/>. Supported conversions:
        /// <see langword="bool"/> (identity), <see langword="long"/> (0 or 1),
        /// <see langword="double"/> (0.0 or 1.0).
        /// </summary>
        /// <exception cref="InvalidCastException"><typeparamref name="TDataType"/> is not a supported conversion.</exception>
        public override TDataType AsType<TDataType>()
        {
            if (typeof(TDataType) == typeof(bool))
            {
                return (TDataType)(object)_val;
            }

            if (typeof(TDataType) == typeof(long))
            {
                return (TDataType)(object)(_val ? 1L : 0L);
            }

            if (typeof(TDataType) == typeof(double))
            {
                return (TDataType)(object)(_val ? 1.0 : 0.0);
            }

            throw new InvalidCastException(GetType(), typeof(TDataType), this);
        }

        /// <summary>Returns <see langword="true"/> only when <typeparamref name="TDataType"/> is <see langword="bool"/>.</summary>
        public override bool IsType<TDataType>()
        {
            return typeof(TDataType) == typeof(bool);
        }

        /// <summary>Returns <c>"True"</c> or <c>"False"</c>.</summary>
        public override string ToString() => _val ? TRUE_STRING : FALSE_STRING;
    }
}
