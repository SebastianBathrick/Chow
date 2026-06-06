namespace Chow.DataTypes
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

        public bool TryMoveNext(out RuntimeValue current)
        {
            if (_index >= _source.Length)
            {
                current = RuntimeValue.None;
                return false;
            }

            current = new RuntimeValue(_source[_index].ToString());
            _index++;
            return true;
        }
    }
}
