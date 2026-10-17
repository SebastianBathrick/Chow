namespace Chow.SourceData
{
    /// <summary>
    /// Base of Chow's iterator objects (Python: the iterator protocol). Iterators are first-class
    /// values: <c>iter()</c> and <c>reversed()</c> return them, <c>next()</c> advances them, and each
    /// <c>for</c> loop drives one. Like Python's, they are single-use: once exhausted they stay so.
    /// </summary>
    /// <remarks>
    /// Combining "advance" and "fetch current" into a single call simplifies the bytecode contract:
    /// the for-loop dispatcher only needs one opcode that pushes the next value or jumps past the body.
    /// </remarks>
    abstract class SourceIterator : SourceObject
    {
        const string ReprFormat = "<{0} object>";

        public override DataType Type => DataType.Iterator;

        /// <summary>The Python type name, e.g. <c>list_iterator</c>.</summary>
        public abstract override string TypeName { get; }

        /// <summary>Advances and produces the next element.</summary>
        /// <param name="current">
        /// The next element when the call returns <see langword="true"/>; otherwise
        /// undefined.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if an element was produced; <see langword="false"/> when the
        /// iterator is exhausted.
        /// </returns>
        public abstract bool TryMoveNext(out SourceValue current);

        // Iterating an iterator continues it rather than starting over: iter(it) is it.
        public override SourceIterator GetIterator()
        {
            return this;
        }

        // Python's `x in it` consumes the iterator up to and including the first match.
        public override bool Contains(SourceValue value)
        {
            while (TryMoveNext(out var current))
            {
                if (SourceValue.IsIdentical(value, current).ToBool() || SourceValue.IsEqual(value, current).ToBool())
                {
                    return true;
                }
            }

            return false;
        }

        public override string ToRepresentation()
        {
            return string.Format(ReprFormat, TypeName);
        }
    }
}
