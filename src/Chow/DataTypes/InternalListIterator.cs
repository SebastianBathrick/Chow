namespace Chow.DataTypes
{
    sealed class InternalListIterator : IChowIterator
    {
        readonly InternalList _list;
        int _index;

        public InternalListIterator(InternalList list)
        {
            _list = list;
            _index = 0;
        }

        public bool TryMoveNext(out ChowValue current)
        {
            if (_index >= _list.Count)
            {
                current = ChowValue.None;
                return false;
            }

            current = _list[_index];
            _index++;
            return true;
        }
    }
}
