using Chow.Interpreter.Exceptions;

namespace Chow.SourceData
{
    /// <summary>
    /// Iterates a dict's keys or values in reverse insertion order. (Python:
    /// <c>dict_reversekeyiterator</c> / <c>dict_reversevalueiterator</c>) A port of CPython 3.11's
    /// <c>dictreviter_iternext</c>, which checks only the dict's size: unlike the forward iterator it
    /// never reports keys changing.
    /// </summary>
    sealed class SourceDictReverseIterator : SourceIterator
    {
        const string KeyIteratorTypeName = "dict_reversekeyiterator";
        const string ValueIteratorTypeName = "dict_reversevalueiterator";

        readonly SourceDict _dict;
        readonly bool _yieldValues;
        int _diUsed;
        int _position;
        bool _isExhausted;

        public SourceDictReverseIterator(SourceDict dict, bool yieldValues)
        {
            _dict = dict;
            _yieldValues = yieldValues;
            _diUsed = dict.Length;
            _position = dict.EntryCount - 1;
        }

        public override string TypeName => _yieldValues ? ValueIteratorTypeName : KeyIteratorTypeName;

        public override bool TryMoveNext(out SourceValue current)
        {
            current = SourceValue.None;

            if (_isExhausted)
            {
                return false;
            }

            if (_diUsed != _dict.Length)
            {
                // Stays invalid so every later step raises too, as in CPython.
                _diUsed = SourceDictIterator.InvalidatedUsed;
                throw new RuntimeErrorException(SourceDictIterator.ChangedSizeError);
            }

            // Reads the dict's current entries each step. A resize can leave the position past the
            // end of a smaller table; CPython reads out of bounds there, so those count as holes.
            for (var slot = _position; slot >= 0; slot--)
            {
                if (slot < _dict.EntryCount && _dict.TryGetEntry(slot, out var key, out var value))
                {
                    _position = slot - 1;
                    current = _yieldValues ? value : key;
                    return true;
                }
            }

            _isExhausted = true;
            return false;
        }
    }
}
