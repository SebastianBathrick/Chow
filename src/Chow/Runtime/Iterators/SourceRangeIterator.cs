namespace Chow.DataTypes
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

        public bool TryMoveNext(out RuntimeValue current)
        {
            if (_step > 0 ? _next >= _stop : _next <= _stop)
            {
                current = RuntimeValue.None;
                return false;
            }

            current = new RuntimeValue(_next);
            _next += _step;
            return true;
        }
    }
}
