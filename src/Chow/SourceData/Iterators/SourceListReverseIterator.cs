namespace Chow.SourceData
{
    /// <summary>
    /// Iterates a list back to front. (Python: <c>list_reverseiterator</c>) Follows CPython's
    /// <c>listreviter_next</c>: if the list shrinks below the current position the iterator ends,
    /// and items appended after it starts are never reached.
    /// </summary>
    sealed class SourceListReverseIterator : SourceIterator
    {
        const string IteratorTypeName = "list_reverseiterator";

        SourceList _list;
        int _index;

        public SourceListReverseIterator(SourceList list)
        {
            _list = list;
            _index = list.Count - 1;
        }

        public override string TypeName => IteratorTypeName;

        public override bool TryMoveNext(out SourceValue current)
        {
            if (_list != null && _index >= 0 && _index < _list.Count)
            {
                current = _list[_index];
                _index--;
                return true;
            }

            _index = -1;
            _list = null;
            current = SourceValue.None;
            return false;
        }
    }
}
