namespace Chow.SourceData
{
    /// <summary>
    /// Iterates a string back to front. (Python: <c>reversed</c>, the generic reverse iterator for
    /// sequences without their own, which among Chow's types is only <c>str</c>)
    /// </summary>
    sealed class SourceReversedIterator : SourceIterator
    {
        const string IteratorTypeName = "reversed";

        readonly string _source;
        int _index;

        public SourceReversedIterator(string source)
        {
            _source = source;
            _index = source.Length - 1;
        }

        public override string TypeName => IteratorTypeName;

        public override bool TryMoveNext(out SourceValue current)
        {
            if (_index < 0)
            {
                current = SourceValue.None;
                return false;
            }

            current = new SourceValue(_source[_index].ToString());
            _index--;
            return true;
        }
    }
}
