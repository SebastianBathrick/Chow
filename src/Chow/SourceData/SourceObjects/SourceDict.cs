using System;
using System.Collections.Generic;
using System.Text;
using Chow.Code;
using Chow.Interpreter.Exceptions;

namespace Chow.SourceData
{
    /// <summary>
    /// Low-level representation of a Python <c>dict</c>, laid out like CPython 3.11's compact dict:
    /// an insertion-ordered entry array (deletions leave holes until the next resize) plus a hash
    /// index of entry positions. Matching CPython's sizing, resizing, and copying rules makes
    /// mutating a dict while iterating it behave exactly as in CPython.
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

        // Sizing and probing constants from CPython 3.11 dictobject.c
        const int MinLog2Size = 3; // PyDict_LOG_MINSIZE
        const int MaxPresizeLog2Size = 17; // log2_max_presize
        const int GrowthRate = 3; // GROWTH_RATE(d) = used * 3
        const int PerturbShift = 5; // PERTURB_SHIFT
        const int IndexEmpty = -1; // DKIX_EMPTY
        const int IndexDummy = -2; // DKIX_DUMMY: a deleted entry's slot, kept so probes still reach later keys

        // compile.c: a literal with more pairs than half of this is built in chunks
        const int StackUseGuideline = 30;

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

        // Py_EMPTY_KEYS's single index slot. Shared and never written: the first insert resizes.
        static readonly int[] EmptyKeysIndices = { IndexEmpty };

        int[] _indices = EmptyKeysIndices; // dk_indices: hash slot -> entry position, IndexEmpty, or IndexDummy
        Entry[] _entries = Array.Empty<Entry>(); // dk_entries
        int _entryCount; // dk_nentries: entries appended since the last resize, holes included
        int _used; // ma_used: live entries
        int _usable; // dk_usable: entries left before a resize
        bool _isStrOnlyTable = true; // DK_IS_UNICODE: CPython's table for exact-str keys, which the empty table counts as

        // Lazily created on first attribute access; most dicts never look up a method.
        Dictionary<string, SourceValue> _methodCache;

        public SourceDict()
        {
        }

        /// <summary>CPython's <c>dict_new_presized</c>, used to build small dict literals.</summary>
        SourceDict(int presize, bool hasOnlyStrKeys)
        {
            if (presize <= UsableFraction(1 << MinLog2Size))
            {
                return;
            }

            Resize(presize > UsableFraction(1 << MaxPresizeLog2Size)
                ? MaxPresizeLog2Size
                : EstimateLog2Size(presize), hasOnlyStrKeys);
        }

        public override DataType Type => DataType.Dict;

        public override bool HasLength => true;

        public override int Length => _used;

        /// <summary>Entries appended since the last resize, holes included.</summary>
        internal int EntryCount => _entryCount;

        bool IsEmptyKeysTable => ReferenceEquals(_indices, EmptyKeysIndices);

        /// <summary>
        /// Builds a <c>{...}</c> literal the way CPython 3.11 compiles one: up to 15 pairs go into a
        /// presized dict, while larger literals are built one key at a time in chunks of 17 pairs
        /// that are then merged together.
        /// </summary>
        public static SourceDict FromLiteral(SourceValue[] keys, SourceValue[] values)
        {
            SourceDict result = null;
            var pending = 0;

            for (var i = 0; i < keys.Length; i++)
            {
                if (pending * 2 > StackUseGuideline)
                {
                    result = MergeLiteralChunk(result, BuildLiteralChunk(keys, values, i - pending, i + 1));
                    pending = 0;
                }
                else
                {
                    pending++;
                }
            }

            if (pending > 0)
            {
                result = MergeLiteralChunk(result, BuildLiteralChunk(keys, values, keys.Length - pending, keys.Length));
            }

            return result ?? new SourceDict();
        }

        public override SourceValue GetItem(SourceValue key)
        {
            PythonKeyComparer.ValidateHashable(key);

            var entryIndex = FindEntry(key, PythonKeyComparer.GetHash(key), out _);

            if (entryIndex >= 0)
            {
                return _entries[entryIndex].Value;
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

        public override SourceIterator GetIterator()
        {
            return new SourceDictIterator(this, yieldValues: false);
        }

        public override SourceIterator GetReversedIterator()
        {
            return new SourceDictReverseIterator(this, yieldValues: false);
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
            Insert(key, value, PythonKeyComparer.GetHash(key));
        }

        public bool ContainsKey(SourceValue key)
        {
            PythonKeyComparer.ValidateHashable(key);
            return FindEntry(key, PythonKeyComparer.GetHash(key), out _) >= 0;
        }

        /// <summary>
        /// Reads the entry at <paramref name="position"/>. Returns <c>false</c> if it is a hole
        /// left by a deletion.
        /// </summary>
        internal bool TryGetEntry(int position, out SourceValue key, out SourceValue value)
        {
            ref var entry = ref _entries[position];
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
                ref var entry = ref _entries[i];

                if (entry.IsHole)
                {
                    continue;
                }

                if (!isFirst)
                {
                    sb.Append(", ");
                }

                (values ? entry.Value : entry.Key).AppendRepresentation(sb);
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

            var entryIndex = FindEntry(key, PythonKeyComparer.GetHash(key), out _);

            if (entryIndex >= 0)
            {
                return _entries[entryIndex].Value;
            }

            return args.Length == 2 ? args[1] : SourceValue.None;
        }

        SourceValue Clear(SourceValue[] args)
        {
            ValidateArgumentCount(args, 0);

            // CPython swaps in the shared empty table rather than emptying the current one.
            _indices = EmptyKeysIndices;
            _entries = Array.Empty<Entry>();
            _entryCount = 0;
            _used = 0;
            _usable = 0;
            _isStrOnlyTable = true;
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
                throw new DataTypeException($"'{DataTypeNames.GetTypeName(args[0])}' object is not a dict");
            }

            MergeFrom((SourceDict)args[0].ToObject());
            return SourceValue.None;
        }

        SourceValue SetDefault(SourceValue[] args)
        {
            ValidateArgRange(args, 1, 2);
            var key = args[0];
            PythonKeyComparer.ValidateHashable(key);

            var hash = PythonKeyComparer.GetHash(key);
            var entryIndex = FindEntry(key, hash, out _);

            if (entryIndex >= 0)
            {
                return _entries[entryIndex].Value;
            }

            var def = args.Length == 2 ? args[1] : SourceValue.None;
            Insert(key, def, hash);
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
                ref var entry = ref a._entries[i];

                if (entry.IsHole)
                {
                    continue;
                }

                var bIndex = b.FindEntry(entry.Key, entry.Hash, out _);

                if (bIndex < 0 || !SourceValue.IsEqual(entry.Value, b._entries[bIndex].Value).ToBool())
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

        static SourceDict BuildLiteralChunk(SourceValue[] keys, SourceValue[] values, int begin, int end)
        {
            var count = end - begin;

            // A large chunk is BUILD_MAP 0 plus one MAP_ADD per pair; smaller ones are presized.
            var chunk = count * 2 > StackUseGuideline
                ? new SourceDict()
                : new SourceDict(count, HasOnlyStrKeys(keys, begin, end));

            for (var i = begin; i < end; i++)
            {
                chunk.Add(keys[i], values[i]);
            }

            return chunk;
        }

        // DICT_UPDATE: each chunk after the first is merged into the first.
        static SourceDict MergeLiteralChunk(SourceDict result, SourceDict chunk)
        {
            if (result == null)
            {
                return chunk;
            }

            result.MergeFrom(chunk);
            return result;
        }

        static bool HasOnlyStrKeys(SourceValue[] keys, int begin, int end)
        {
            for (var i = begin; i < end; i++)
            {
                if (keys[i].DataType != DataType.Str)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>CPython's <c>PyDict_Copy</c>.</summary>
        SourceDict Copy()
        {
            var copy = new SourceDict();

            if (_used == 0)
            {
                return copy;
            }

            // Fast copy keeps holes, but only when at most a third of the entries are holes.
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
                && (other._indices.Length == 1 << MinLog2Size || UsableFraction(other._indices.Length / 2) < other._used))
            {
                CloneFrom(other);
                return;
            }

            // One big resize up front instead of growing incrementally.
            if (UsableFraction(_indices.Length) < other._used)
            {
                Resize(EstimateLog2Size((long)_used + other._used), other._isStrOnlyTable);
            }

            for (var i = 0; i < other._entryCount; i++)
            {
                ref var entry = ref other._entries[i];

                if (!entry.IsHole)
                {
                    Insert(entry.Key, entry.Value, entry.Hash);
                }
            }
        }

        // clone_combined_dict_keys: an exact copy of the table, holes included.
        void CloneFrom(SourceDict other)
        {
            _indices = (int[])other._indices.Clone();
            _entries = (Entry[])other._entries.Clone();
            _entryCount = other._entryCount;
            _used = other._used;
            _usable = other._usable;
            _isStrOnlyTable = other._isStrOnlyTable;
        }

        // insertdict, for a key already validated and hashed
        void Insert(SourceValue key, SourceValue value, int hash)
        {
            var isStrKey = key.DataType == DataType.Str;

            // insert_to_emptydict: the first key decides whether the new table is str-only.
            if (IsEmptyKeysTable)
            {
                Resize(MinLog2Size, isStrKey);
                AppendEntry(key, value, hash);
                return;
            }

            // A str-only table is rebuilt as a general one before it takes a non-str key.
            if (_isStrOnlyTable && !isStrKey)
            {
                InsertionResize(isStrOnly: false);
            }

            // An existing key keeps its original key object; only the value changes.
            var entryIndex = FindEntry(key, hash, out _);

            if (entryIndex >= 0)
            {
                _entries[entryIndex].Value = value;
                return;
            }

            if (_usable <= 0)
            {
                InsertionResize(isStrOnly: true);
            }

            AppendEntry(key, value, hash);
        }

        void AppendEntry(SourceValue key, SourceValue value, int hash)
        {
            _indices[FindFreeIndexSlot(hash)] = _entryCount;
            _entries[_entryCount] = new Entry(key, value, hash);
            _entryCount++;
            _used++;
            _usable--;
        }

        bool TryRemove(SourceValue key, out SourceValue value)
        {
            var entryIndex = FindEntry(key, PythonKeyComparer.GetHash(key), out var indexSlot);

            if (entryIndex < 0)
            {
                value = SourceValue.None;
                return false;
            }

            value = _entries[entryIndex].Value;

            // The entry becomes a hole; usable entries are not given back until the next resize.
            _indices[indexSlot] = IndexDummy;
            _entries[entryIndex] = Entry.Hole;
            _used--;
            return true;
        }

        // lookdict: the position of the entry holding key, or -1. indexSlot is its hash slot.
        int FindEntry(SourceValue key, int hash, out int indexSlot)
        {
            var mask = (uint)_indices.Length - 1;
            var perturb = (uint)hash;
            var slot = (uint)hash & mask;

            while (true)
            {
                var entryIndex = _indices[slot];

                if (entryIndex == IndexEmpty)
                {
                    indexSlot = -1;
                    return -1;
                }

                if (entryIndex >= 0)
                {
                    ref var entry = ref _entries[entryIndex];

                    if (entry.Hash == hash && PythonKeyComparer.AreEqual(entry.Key, key))
                    {
                        indexSlot = (int)slot;
                        return entryIndex;
                    }
                }

                perturb >>= PerturbShift;
                slot = (slot * 5 + perturb + 1) & mask;
            }
        }

        // find_empty_slot: the first empty or dummy hash slot on the probe path. One always exists,
        // because entries (holes included) never exceed two thirds of the slots.
        int FindFreeIndexSlot(int hash)
        {
            var mask = (uint)_indices.Length - 1;
            var perturb = (uint)hash;
            var slot = (uint)hash & mask;

            while (_indices[slot] >= 0)
            {
                perturb >>= PerturbShift;
                slot = (slot * 5 + perturb + 1) & mask;
            }

            return (int)slot;
        }

        // insertion_resize
        void InsertionResize(bool isStrOnly)
        {
            Resize(CalculateLog2Size((long)_used * GrowthRate), isStrOnly);
        }

        // dictresize: moves live entries into a 2^log2Size table, dropping holes. Cached hashes mean
        // no key is rehashed. A general table never becomes str-only again.
        void Resize(int log2Size, bool isStrOnly)
        {
            var size = 1 << log2Size;
            var oldEntries = _entries;
            var oldCount = _entryCount;

            _indices = new int[size];

            for (var i = 0; i < size; i++)
            {
                _indices[i] = IndexEmpty;
            }

            _entries = new Entry[UsableFraction(size)];
            _entryCount = 0;

            for (var i = 0; i < oldCount; i++)
            {
                if (oldEntries[i].IsHole)
                {
                    continue;
                }

                _indices[FindFreeIndexSlot(oldEntries[i].Hash)] = _entryCount;
                _entries[_entryCount] = oldEntries[i];
                _entryCount++;
            }

            _usable = UsableFraction(size) - _entryCount;
            _isStrOnlyTable = _isStrOnlyTable && isStrOnly;
        }

        // USABLE_FRACTION
        static int UsableFraction(int size)
        {
            return (size << 1) / 3;
        }

        // calculate_log2_keysize. CPython's 64-bit builds compute bit_length((minSize | 8) - 1), which
        // is not quite "the smallest table holding minSize": it gives 16 for 1-7 and doubles exact
        // powers of two from 16 up. Reproduced as-is, since table sizes decide when a dict resizes.
        static int CalculateLog2Size(long minSize)
        {
            var bits = (minSize | (1 << MinLog2Size)) - 1;
            var log2Size = 0;

            while (bits > 0)
            {
                log2Size++;
                bits >>= 1;
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
                ref var entry = ref _entries[i];

                if (entry.IsHole)
                {
                    continue;
                }

                if (!isFirst)
                {
                    sb.Append(", ");
                }

                entry.Key.AppendRepresentation(sb);
                sb.Append(": ");
                entry.Value.AppendRepresentation(sb);
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
            public int Hash; // cached so resizes and merges never rehash
            public bool IsHole;

            public Entry(SourceValue key, SourceValue value, int hash)
            {
                Key = key;
                Value = value;
                Hash = hash;
                IsHole = false;
            }
        }
    }
}
