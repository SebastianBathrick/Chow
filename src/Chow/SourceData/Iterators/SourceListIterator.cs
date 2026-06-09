namespace Chow.SourceData
{
    sealed class SourceListIterator : IIterator
    {
        readonly SourceList _list;
        int _index;

        public SourceListIterator(SourceList list)
        {
            _list = list;
            _index = 0;
        }

        public bool TryMoveNext(out SourceValue current)
        {
            if (_index >= _list.Count)
            {
                current = SourceValue.None;
                return false;
            }

            current = _list[_index];
            _index++;
            return true;
        }
    }
}
