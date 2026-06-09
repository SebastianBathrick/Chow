namespace Chow.SourceData
{
    sealed class SourceRangeIterator : IIterator
    {
        readonly long _stop;
        readonly long _step;
        long _next;

        public SourceRangeIterator(SourceRange range)
        {
            _next = range.Start;
            _stop = range.Stop;
            _step = range.Step;
        }

        public bool TryMoveNext(out SourceValue current)
        {
            if (_step > 0 ? _next >= _stop : _next <= _stop)
            {
                current = SourceValue.None;
                return false;
            }

            current = new SourceValue(_next);
            _next += _step;
            return true;
        }
    }
}
