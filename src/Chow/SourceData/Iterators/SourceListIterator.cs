namespace Chow.SourceData
{
    /// <summary>Iterates a list front to back. (Python: <c>list_iterator</c>)</summary>
    sealed class SourceListIterator : SourceIterator
    {
        const string IteratorTypeName = "list_iterator";

        // Dropped once exhausted, so items appended later don't revive the iterator.
        SourceList _list;
        int _index;

        public SourceListIterator(SourceList list)
        {
            _list = list;
            _index = 0;
        }

        public override string TypeName => IteratorTypeName;

        public override bool TryMoveNext(out SourceValue current)
        {
            if (_list != null && _index < _list.Count)
            {
                current = _list[_index];
                _index++;
                return true;
            }

            _list = null;
            current = SourceValue.None;
            return false;
        }
    }
}
