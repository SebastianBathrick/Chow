namespace Chow.Interpreter.State.Values
{
    sealed class InternalRangeIterator : IChowIterator
    {
        readonly long _stop;
        readonly long _step;
        long _next;

        public InternalRangeIterator(InternalRange range)
        {
            _next = range.Start;
            _stop = range.Stop;
            _step = range.Step;
        }

        public bool TryMoveNext(out ChowValue current)
        {
            if (_step > 0 ? _next >= _stop : _next <= _stop)
            {
                current = ChowValue.None;
                return false;
            }

            current = new ChowValue(_next);
            _next += _step;
            return true;
        }
    }
}
