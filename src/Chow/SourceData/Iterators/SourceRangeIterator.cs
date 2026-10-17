namespace Chow.SourceData
{
    /// <summary>
    /// Iterates a range forward or in reverse. (Python: <c>range_iterator</c>) Counts down the
    /// remaining values like CPython's, so stepping past the last value can't overflow the bounds check.
    /// </summary>
    sealed class SourceRangeIterator : SourceIterator
    {
        const string IteratorTypeName = "range_iterator";

        readonly long _step;
        long _next;
        int _remaining;

        public SourceRangeIterator(SourceRange range, bool isReversed)
        {
            _remaining = range.Count;

            if (isReversed)
            {
                // CPython's range_reverse: start from the last value and step back toward the first.
                _next = unchecked(range.Start + (_remaining - 1) * range.Step);
                _step = unchecked(-range.Step);
            }
            else
            {
                _next = range.Start;
                _step = range.Step;
            }
        }

        public override string TypeName => IteratorTypeName;

        public override bool TryMoveNext(out SourceValue current)
        {
            if (_remaining <= 0)
            {
                current = SourceValue.None;
                return false;
            }

            current = new SourceValue(_next);
            _next = unchecked(_next + _step);
            _remaining--;
            return true;
        }
    }
}
