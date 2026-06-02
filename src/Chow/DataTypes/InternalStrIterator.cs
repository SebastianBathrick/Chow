namespace Chow.DataTypes
{
    sealed class InternalStrIterator : IChowIterator
    {
        readonly string _source;
        int _index;

        public InternalStrIterator(string source)
        {
            _source = source;
            _index = 0;
        }

        public bool TryMoveNext(out ChowValue current)
        {
            if (_index >= _source.Length)
            {
                current = ChowValue.None;
                return false;
            }

            current = new ChowValue(_source[_index].ToString());
            _index++;
            return true;
        }
    }
}
