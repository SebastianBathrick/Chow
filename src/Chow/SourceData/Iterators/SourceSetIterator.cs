using Chow.Interpreter.Exceptions;

namespace Chow.SourceData
{
    /// <summary>
    /// Iterates a set or frozenset in table order. (Python: <c>set_iterator</c>) A port of CPython
    /// 3.11's <c>setiter_iternext</c>, which checks only the set's size, never for swapped elements.
    /// </summary>
    sealed class SourceSetIterator : SourceIterator
    {
        const string IteratorTypeName = "set_iterator";
        const string ChangedSizeError = "Set changed size during iteration";
        const int InvalidatedUsed = -1;

        // Dropped once exhausted, as CPython does, so later additions don't revive the iterator.
        SourceSet _set;
        int _siUsed;
        int _position;

        public SourceSetIterator(SourceSet set)
        {
            _set = set;
            _siUsed = set.Length;
        }

        public override string TypeName => IteratorTypeName;

        public override bool TryMoveNext(out SourceValue current)
        {
            current = SourceValue.None;

            if (_set == null)
            {
                return false;
            }

            if (_siUsed != _set.Length)
            {
                // Stays invalid so every later step raises too, as in CPython.
                _siUsed = InvalidatedUsed;
                throw new RuntimeErrorException(ChangedSizeError);
            }

            // Reads the set's current table each step: a resize replaces it.
            var mask = _set.Mask;
            var slot = _position;
            SourceValue key = default;

            while (slot <= mask && !_set.TryGetActive(slot, out key))
            {
                slot++;
            }

            _position = slot + 1;

            if (slot > mask)
            {
                _set = null;
                return false;
            }

            current = key;
            return true;
        }
    }
}
