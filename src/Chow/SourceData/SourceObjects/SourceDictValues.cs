namespace Chow.SourceData
{
    /// <summary>
    /// Live view of a dict's values. (Python: <c>dict_values</c>) Unlike keys, it compares and
    /// hashes by identity.
    /// </summary>
    sealed class SourceDictValues : SourceObject
    {
        const string ReprFormat = "dict_values({0})";

        readonly SourceDict _dict;

        public SourceDictValues(SourceDict dict)
        {
            _dict = dict;
        }

        public override DataType Type => DataType.DictValues;

        public override bool HasLength => true;

        public override int Length => _dict.Length;

        // Python containers check identity before ==.
        public override bool Contains(SourceValue value)
        {
            var iterator = GetIterator();

            while (iterator.TryMoveNext(out var current))
            {
                if (SourceValue.IsIdentical(value, current).ToBool() || SourceValue.IsEqual(value, current).ToBool())
                {
                    return true;
                }
            }

            return false;
        }

        public override SourceIterator GetIterator()
        {
            return new SourceDictIterator(_dict, yieldValues: true);
        }

        public override SourceIterator GetReversedIterator()
        {
            return new SourceDictReverseIterator(_dict, yieldValues: true);
        }

        public override string ToRepresentation()
        {
            return string.Format(ReprFormat, _dict.ToListRepresentation(values: true));
        }
    }
}
