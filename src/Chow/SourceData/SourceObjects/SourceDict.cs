using System;
using System.Collections.Generic;
using System.Text;
using Chow.Code;
using Chow.Interpreter.Exceptions;

namespace Chow.SourceData
{
    /// <summary>
    /// Low-level representation of a Python <c>dict</c>. Entries are laid out like CPython 3.11's
    /// compact dict (an insertion-ordered array where deletions leave holes, compacted only on
    /// resize) so that mutating a dict while iterating it behaves exactly as in CPython.
    /// </summary>
    class SourceDict : SourceObject
    {
        const string GetMethodName = "get";
        const string ClearMethodName = "clear";
        const string PopMethodName = "pop";
        const string UpdateMethodName = "update";
        const string SetMethodName = "setdefault";
        const string KeysMethodName = "keys";
        const string ValuesMethodName = "values";

        // Sizing constants from CPython 3.11 dictobject.c
        const int MinLog2Size = 3; // PyDict_LOG_MINSIZE
        const int MaxPresizeLog2Size = 17; // log2_max_presize
        const int EmptyKeysLog2Size = 0; // Py_EMPTY_KEYS, the table a dict starts with
        const int GrowthRate = 3; // GROWTH_RATE(d) = used * 3

        static readonly List<string> MethodNames = new List<string>
        {
            GetMethodName,
            ClearMethodName,
            PopMethodName,
            UpdateMethodName,
            SetMethodName,
            KeysMethodName,
            ValuesMethodName
        };

        // Maps each key to its slot in _entries.
        readonly Dictionary<SourceValue, int> _index = new Dictionary<SourceValue, int>(PythonKeyComparer.Instance);

        Entry[] _entries = Array.Empty<Entry>();
        int _entryCount; // dk_nentries: slots in use, holes included
        int _used; // ma_used: live entries
        int _usable; // dk_usable: slots left before a resize
        int _log2Size = EmptyKeysLog2Size;

        // Lazily created on first attribute access; most dicts never look up a method.
        Dictionary<string, SourceValue> _methodCache;

        public SourceDict()
        {
        }

        /// <summary>
        /// Creates a dict sized for <paramref name="presize"/> entries, as CPython's
        /// <c>dict_new_presized</c> does for a <c>{...}</c> literal.
        /// </summary>
        public SourceDict(int presize)
        {
            if (presize <= UsableFraction(1 << MinLog2Size))
            {
                return;
            }

            Resize(presize > UsableFraction(1 << MaxPresizeLog2Size)
                ? MaxPresizeLog2Size
                : EstimateLog2Size(presize));
        }

        public override DataType Type => DataType.Dict;

        public override bool HasLength => true;

        public override int Length => _used;

        /// <summary>Slots in the entry array, holes included.</summary>
        internal int EntryCount => _entryCount;

        public override SourceValue GetItem(SourceValue key)
        {
            PythonKeyComparer.ValidateHashable(key);

            if (_index.TryGetValue(key, out var slot))
            {
                return _entries[slot].Value;
            }

            throw new SubscriptException(key.ToRepresentation());
        }

        public override void SetItem(SourceValue key, SourceValue value)
        {
            Add(key, value);
        }

        public override void DeleteItem(SourceValue key)
        {
            PythonKeyComparer.ValidateHashable(key);

            if (!TryRemove(key, out _))
            {
                throw new SubscriptException(key.ToRepresentation());
            }
        }

        public override bool Contains(SourceValue value)
        {
            return ContainsKey(value);
        }

        public override IIterator GetIterator()
        {
            return new SourceDictIterator(this, yieldValues: false);
        }

        public override SourceValue GetAttribute(SourceValue name)
        {
            var methodName = name.ToString();
            _methodCache = _methodCache ?? new Dictionary<string, SourceValue>();

            if (_methodCache.TryGetValue(methodName, out var method))
            {
                return method;
            }

            method = new SourceValue(GetMethod(methodName));
            _methodCache[methodName] = method;

            return method;
        }

        public override List<string> Directory => MethodNames;

        public override bool EqualsTo(SourceObject other)
        {
            return other is SourceDict dict && ElementsEqual(this, dict);
        }

        public void Add(SourceValue key, SourceValue value)
        {
            PythonKeyComparer.ValidateHashable(key);

            // An existing key keeps its original key object; only the value changes.
            if (_index.TryGetValue(key, out var slot))
            {
                _entries[slot].Value = value;
                return;
            }

            if (_usable <= 0)
            {
                Resize(CalculateLog2Size((long)_used * GrowthRate));
            }

            _entries[_entryCount] = new Entry(key, value);
            _index[key] = _entryCount;
            _entryCount++;
            _used++;
            _usable--;
        }

        public bool ContainsKey(SourceValue key)
        {
            PythonKeyComparer.ValidateHashable(key);
            return _index.ContainsKey(key);
        }

        /// <summary>
        /// Reads the entry in <paramref name="slot"/>. Returns <c>false</c> if the slot is a hole
        /// left by a deletion.
        /// </summary>
        internal bool TryGetEntry(int slot, out SourceValue key, out SourceValue value)
        {
            var entry = _entries[slot];
            key = entry.Key;
            value = entry.Value;
            return !entry.IsHole;
        }

        /// <summary>The dict's keys or values formatted like a list repr, e.g. <c>['a', 'b']</c>.</summary>
        internal string ToListRepresentation(bool values)
        {
            var sb = new StringBuilder();
            sb.Append('[');

            var isFirst = true;

            for (var i = 0; i < _entryCount; i++)
            {
                if (!TryGetEntry(i, out var key, out var value))
                {
                    continue;
                }

                if (!isFirst)
                {
                    sb.Append(", ");
                }

                sb.Append((values ? value : key).ToRepresentation());
                isFirst = false;
            }

            sb.Append(']');
            return sb.ToString();
        }

        SourceValue Get(SourceValue[] args)
        {
            ValidateArgRange(args, 1, 2);
            var key = args[0];
            PythonKeyComparer.ValidateHashable(key);

            if (_index.TryGetValue(key, out var slot))
            {
                return _entries[slot].Value;
            }

            return args.Length == 2 ? args[1] : SourceValue.None;
        }

        SourceValue Clear(SourceValue[] args)
        {
            ValidateArgumentCount(args, 0);

            // CPython swaps in a fresh empty table rather than emptying the current one.
            _index.Clear();
            _entries = Array.Empty<Entry>();
            _entryCount = 0;
            _used = 0;
            _usable = 0;
            _log2Size = EmptyKeysLog2Size;
            return SourceValue.None;
        }

        SourceValue Pop(SourceValue[] args)
        {
            ValidateArgRange(args, 1, 2);
            var key = args[0];
            PythonKeyComparer.ValidateHashable(key);

            if (TryRemove(key, out var value))
            {
                return value;
            }

            return args.Length == 2 ? args[1] : throw new SubscriptException(key.ToRepresentation());
        }

        SourceValue Update(SourceValue[] args)
        {
            ValidateArgumentCount(args, 1);

            if (args[0].DataType != DataType.Dict)
            {
                throw new DataTypeException($"'{DataTypeNames.GetTypeName(args[0].DataType)}' object is not a dict");
            }

            MergeFrom((SourceDict)args[0].ToObject());
            return SourceValue.None;
        }

        SourceValue SetDefault(SourceValue[] args)
        {
            ValidateArgRange(args, 1, 2);
            var key = args[0];
            PythonKeyComparer.ValidateHashable(key);

            if (_index.TryGetValue(key, out var slot))
            {
                return _entries[slot].Value;
            }

            var def = args.Length == 2 ? args[1] : SourceValue.None;
            Add(key, def);
            return def;
        }

        SourceValue Keys(SourceValue[] args)
        {
            ValidateArgumentCount(args, 0);
            return new SourceValue(new SourceDictKeys(this));
        }

        SourceValue Values(SourceValue[] args)
        {
            ValidateArgumentCount(args, 0);
            return new SourceValue(new SourceDictValues(this));
        }

        public Func<SourceValue[], SourceValue> GetMethod(string methodName)
        {
            switch (methodName)
            {
                case GetMethodName:
                    return Get;
                case ClearMethodName:
                    return Clear;
                case PopMethodName:
                    return Pop;
                case UpdateMethodName:
                    return Update;
                case SetMethodName:
                    return SetDefault;
                case KeysMethodName:
                    return Keys;
                case ValuesMethodName:
                    return Values;
            }

            throw new NotImplementedException($"Method '{methodName}' is not implemented for SourceDict");
        }

        public static bool ElementsEqual(SourceDict a, SourceDict b)
        {
            if (a._used != b._used)
            {
                return false;
            }

            for (var i = 0; i < a._entryCount; i++)
            {
                if (!a.TryGetEntry(i, out var key, out var aValue))
                {
                    continue;
                }

                if (!b._index.TryGetValue(key, out var bSlot))
                {
                    return false;
                }

                if (!SourceValue.IsEqual(aValue, b._entries[bSlot].Value).ToBool())
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary><c>a | b</c>: a copy of <paramref name="a"/> updated with <paramref name="b"/>.</summary>
        public static SourceDict Merge(SourceDict a, SourceDict b)
        {
            var result = a.Copy();
            result.MergeFrom(b);
            return result;
        }

        /// <summary>CPython's <c>PyDict_Copy</c>.</summary>
        SourceDict Copy()
        {
            var copy = new SourceDict();

            if (_used == 0)
            {
                return copy;
            }

            // Fast copy keeps holes, but only when at most a third of the slots are holes.
            if (_used >= _entryCount * 2 / 3)
            {
                copy.CloneFrom(this);
                return copy;
            }

            copy.MergeFrom(this);
            return copy;
        }

        /// <summary>CPython's <c>dict_merge</c> with override, used by <c>update</c>, <c>dict()</c>, and <c>|</c>.</summary>
        void MergeFrom(SourceDict other)
        {
            if (ReferenceEquals(other, this) || other._used == 0)
            {
                return;
            }

            // An empty target clones a hole-free source that is either minimum size or not oversized.
            if (_used == 0
                && other._used == other._entryCount
                && (other._log2Size == MinLog2Size || UsableFraction((1 << other._log2Size) / 2) < other._used))
            {
                CloneFrom(other);
                return;
            }

            // One big resize up front instead of growing incrementally.
            if (UsableFraction(1 << _log2Size) < other._used)
            {
                Resize(EstimateLog2Size((long)_used + other._used));
            }

            for (var i = 0; i < other._entryCount; i++)
            {
                if (other.TryGetEntry(i, out var key, out var value))
                {
                    Add(key, value);
                }
            }
        }

        // clone_combined_dict_keys: an exact copy of the table, holes included.
        void CloneFrom(SourceDict other)
        {
            _entries = (Entry[])other._entries.Clone();
            _entryCount = other._entryCount;
            _used = other._used;
            _usable = other._usable;
            _log2Size = other._log2Size;

            _index.Clear();

            for (var i = 0; i < _entryCount; i++)
            {
                if (TryGetEntry(i, out var key, out _))
                {
                    _index[key] = i;
                }
            }
        }

        bool TryRemove(SourceValue key, out SourceValue value)
        {
            if (!_index.TryGetValue(key, out var slot))
            {
                value = SourceValue.None;
                return false;
            }

            value = _entries[slot].Value;
            _index.Remove(key);

            // The slot becomes a hole; usable slots are not given back until the next resize.
            _entries[slot] = Entry.Hole;
            _used--;
            return true;
        }

        // dictresize: moves live entries into a new table of 2^log2Size, dropping holes.
        void Resize(int log2Size)
        {
            var size = 1 << log2Size;
            var newEntries = new Entry[UsableFraction(size)];
            var count = 0;

            _index.Clear();

            for (var i = 0; i < _entryCount; i++)
            {
                if (!_entries[i].IsHole)
                {
                    newEntries[count] = _entries[i];
                    _index[newEntries[count].Key] = count;
                    count++;
                }
            }

            _entries = newEntries;
            _entryCount = count;
            _log2Size = log2Size;
            _usable = UsableFraction(size) - count;
        }

        // USABLE_FRACTION
        static int UsableFraction(int size)
        {
            return (size << 1) / 3;
        }

        // calculate_log2_keysize: the smallest table (at least 8) holding minSize.
        static int CalculateLog2Size(long minSize)
        {
            var log2Size = MinLog2Size;

            while ((1L << log2Size) < minSize)
            {
                log2Size++;
            }

            return log2Size;
        }

        // estimate_log2_keysize
        static int EstimateLog2Size(long entries)
        {
            return CalculateLog2Size((entries * 3 + 1) / 2);
        }

        public override string ToRepresentation()
        {
            var sb = new StringBuilder();
            sb.Append('{');

            var isFirst = true;

            for (var i = 0; i < _entryCount; i++)
            {
                if (!TryGetEntry(i, out var key, out var value))
                {
                    continue;
                }

                if (!isFirst)
                {
                    sb.Append(", ");
                }

                sb.Append($"{key.ToRepresentation()}: {value.ToRepresentation()}");
                isFirst = false;
            }

            sb.Append('}');
            return sb.ToString();
        }

        static void ValidateArgumentCount(SourceValue[] args, int expectedCount)
        {
            var actualCount = args?.Length ?? 0;

            if (actualCount == expectedCount)
            {
                return;
            }

            throw new DataTypeException(
                $"Method requires {expectedCount} arguments, but {actualCount} were provided");
        }

        static void ValidateArgRange(SourceValue[] args, int min, int max)
        {
            var actualCount = args?.Length ?? 0;

            if (actualCount >= min && actualCount <= max)
            {
                return;
            }

            throw new DataTypeException(
                $"Method requires between {min} and {max} arguments, but "
                + $"{actualCount} were provided");

        }

        struct Entry
        {
            public static readonly Entry Hole = new Entry { IsHole = true };

            public SourceValue Key;
            public SourceValue Value;
            public bool IsHole;

            public Entry(SourceValue key, SourceValue value)
            {
                Key = key;
                Value = value;
                IsHole = false;
            }
        }
    }
}
