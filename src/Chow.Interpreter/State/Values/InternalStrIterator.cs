namespace Chow.Interpreter.State.Values
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

        public bool TryMoveNext(out TaggedUnion current)
        {
            if (_index >= _source.Length)
            {
                current = TaggedUnion.None;
                return false;
            }

            current = new TaggedUnion(_source[_index].ToString());
            _index++;
            return true;
        }
    }
}
