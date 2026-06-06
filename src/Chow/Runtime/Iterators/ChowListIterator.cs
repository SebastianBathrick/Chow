namespace Chow.DataTypes
{
    sealed class ChowListIterator : IIterator
    {
        readonly ChowList _list;
        int _index;

        public ChowListIterator(ChowList list)
        {
            _list = list;
            _index = 0;
        }

        public bool TryMoveNext(out TaggedUnion current)
        {
            if (_index >= _list.Count)
            {
                current = TaggedUnion.None;
                return false;
            }

            current = _list[_index];
            _index++;
            return true;
        }
    }
}
