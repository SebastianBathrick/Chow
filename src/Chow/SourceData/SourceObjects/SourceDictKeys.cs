namespace Chow.SourceData
{
    /// <summary>
    /// Live view of a dict's keys. (Python: <c>dict_keys</c>) Reflects later changes to the dict
    /// and compares like a set.
    /// </summary>
    sealed class SourceDictKeys : SourceObject
    {
        const string ReprFormat = "dict_keys({0})";

        readonly SourceDict _dict;

        public SourceDictKeys(SourceDict dict)
        {
            _dict = dict;
        }

        public override DataType Type => DataType.DictKeys;

        public override bool HasLength => true;

        public override int Length => _dict.Length;

        public override bool Contains(SourceValue value)
        {
            return _dict.ContainsKey(value);
        }

        public override SourceIterator GetIterator()
        {
            return new SourceDictIterator(_dict, yieldValues: false);
        }

        public override SourceIterator GetReversedIterator()
        {
            return new SourceDictReverseIterator(_dict, yieldValues: false);
        }

        public override bool EqualsTo(SourceObject other)
        {
            if (!(other is SourceDictKeys keys) || keys.Length != Length)
            {
                return false;
            }

            var iterator = GetIterator();

            while (iterator.TryMoveNext(out var key))
            {
                if (!keys.Contains(key))
                {
                    return false;
                }
            }

            return true;
        }

        public override string ToRepresentation()
        {
            return string.Format(ReprFormat, _dict.ToListRepresentation(values: false));
        }
    }
}
