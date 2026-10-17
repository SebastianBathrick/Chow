namespace Chow.SourceData
{
    /// <summary>
    /// Iterates a string's characters. (Python: <c>str_ascii_iterator</c> for ASCII strings,
    /// otherwise <c>str_iterator</c>)
    /// </summary>
    sealed class SourceStringIterator : SourceIterator
    {
        const string AsciiIteratorTypeName = "str_ascii_iterator";
        const string IteratorTypeName = "str_iterator";
        const char LastAsciiChar = '\x7f';

        readonly string _source;
        int _index;

        public SourceStringIterator(string source)
        {
            _source = source;
            _index = 0;
        }

        // Worked out on demand rather than up front, so plain loops never scan the string for it.
        public override string TypeName => IsAscii(_source) ? AsciiIteratorTypeName : IteratorTypeName;

        public override bool TryMoveNext(out SourceValue current)
        {
            if (_index >= _source.Length)
            {
                current = SourceValue.None;
                return false;
            }

            current = new SourceValue(_source[_index].ToString());
            _index++;
            return true;
        }

        static bool IsAscii(string value)
        {
            foreach (var ch in value)
            {
                if (ch > LastAsciiChar)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
