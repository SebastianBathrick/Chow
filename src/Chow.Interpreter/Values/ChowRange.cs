using Chow.Interpreter.State.Values;
using System;

namespace Chow.Interpreter.Values
{
    /// <summary>
    /// Represents a Chow <c>range</c> value: an immutable, lazy integer sequence defined by
    /// <see cref="Start"/>, <see cref="Stop"/>, and <see cref="Step"/>. Mirrors Python's
    /// <c>range</c> object semantics.
    /// </summary>
    public class ChowRange : ChowValue
    {
        internal InternalRange Internal { get; }

        /// <summary>Gets the inclusive starting value of the sequence.</summary>
        public long Start => Internal.Start;

        /// <summary>Gets the exclusive stopping value of the sequence.</summary>
        public long Stop => Internal.Stop;

        /// <summary>Gets the step between successive elements. May be negative; never zero.</summary>
        public long Step => Internal.Step;

        /// <summary>Gets the number of elements that the range will yield when iterated.</summary>
        public int Count => Internal.Count;

        /// <summary>Initialises a <see cref="ChowRange"/> with the given start, stop, and step.</summary>
        /// <param name="start">Inclusive starting value.</param>
        /// <param name="stop">Exclusive stopping value.</param>
        /// <param name="step">Step between successive elements. Must not be zero.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="step"/> is zero.</exception>
        public ChowRange(long start, long stop, long step)
        {
            Internal = new InternalRange(start, stop, step);
        }

        internal ChowRange(InternalRange wrapped)
        {
            Internal = wrapped;
        }

        /// <summary>
        /// Extracts the underlying value as <typeparamref name="TDataType"/>. Supported conversions:
        /// <see langword="bool"/> (<see langword="true"/> when the range is non-empty).
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

        /// <summary>Returns the string representation of the range, matching Python's <c>repr</c>.</summary>
        public override string ToString() => Internal.ToString();
    }
}
