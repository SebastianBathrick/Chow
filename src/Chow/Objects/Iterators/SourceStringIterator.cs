namespace Chow.Objects
{
    sealed class SourceStringIterator : IIterator
    {
        readonly string _source;
        int _index;

        public SourceStringIterator(string source)
        {
            _source = source;
            _index = 0;
        }

        public bool TryMoveNext(out SourceValue current)
        {
            if (_index >= _source.Length)
            {
                current = SourceValue.None;
                return false;
            }

            current = new SourceValue(_source[_index].ToString());
            _index++;
            return true;
        }
    }
}
