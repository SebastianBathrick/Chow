using Chow.Interpreter.Exceptions;

namespace Chow.SourceData
{
    /// <summary>
    /// Iterates a dict's keys or values. (Python: <c>dict_keyiterator</c> / <c>dict_valueiterator</c>)
    /// A port of CPython 3.11's <c>dictiter_iternextkey</c>, so mutating the dict mid-loop raises (or
    /// doesn't) exactly when CPython does.
    /// </summary>
    sealed class SourceDictIterator : SourceIterator
    {
        internal const string ChangedSizeError = "dictionary changed size during iteration";
        internal const int InvalidatedUsed = -1;
        const string KeysChangedError = "dictionary keys changed during iteration";
        const string KeyIteratorTypeName = "dict_keyiterator";
        const string ValueIteratorTypeName = "dict_valueiterator";

        readonly SourceDict _dict;
        readonly bool _yieldValues;
        int _diUsed;
        int _remaining;
        int _position;
        bool _isExhausted;

        public SourceDictIterator(SourceDict dict, bool yieldValues)
        {
            _dict = dict;
            _yieldValues = yieldValues;
            _diUsed = dict.Length;
            _remaining = dict.Length;
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
                _diUsed = InvalidatedUsed;
                throw new RuntimeErrorException(ChangedSizeError);
            }

            // Reads the dict's current entries each step: a resize or clear replaces them.
            var slot = _position;
            SourceValue key;
            SourceValue value;

            while (true)
            {
                if (slot >= _dict.EntryCount)
                {
                    _isExhausted = true;
                    return false;
                }

                if (_dict.TryGetEntry(slot, out key, out value))
                {
                    break;
                }

                slot++;
            }

            // An entry the starting size didn't account for means keys were swapped in.
            if (_remaining == 0)
            {
                _isExhausted = true;
                throw new RuntimeErrorException(KeysChangedError);
            }

            _position = slot + 1;
            _remaining--;
            current = _yieldValues ? value : key;
            return true;
        }
    }
}
