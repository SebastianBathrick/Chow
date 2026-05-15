using System;

namespace Chow.Interpreter.Values
{
    /// <summary>
    /// Represents the Chow <c>None</c> value. Use <see cref="ChowValue.None"/> to obtain the singleton
    /// instance rather than constructing this type directly.
    /// </summary>
    public class ChowNone : ChowValue
    {
        const string NONE_STRING = "None";

        internal static ChowValue Instance { get; } = new ChowNone();

        // Only one instance of ChowNone should exist
        ChowNone()
        {
            if (Instance == null)
            {
                return;
            }

            throw new InvalidOperationException("Only one instance of ChowNone should exist.");
        }

        /// <inheritdoc/>
        /// <exception cref="InvalidCastException">Always thrown — <c>None</c> has no supported conversions.</exception>
        public override TDataType AsType<TDataType>()
        {
            throw new InvalidCastException(GetType(), typeof(TDataType), this);
        }

        /// <summary>Always returns <see langword="false"/>.</summary>
        public override bool IsType<TDataType>()
        {
            return false;
        }

        /// <summary>Returns <c>"None"</c>.</summary>
        public override string ToString() => NONE_STRING;
    }
}
