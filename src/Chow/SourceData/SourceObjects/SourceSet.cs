using System;
using System.Collections.Generic;
using System.Text;
using Chow.Code;
using Chow.Interpreter.Exceptions;

namespace Chow.SourceData
{
    /// <summary>
    /// Low-level representation of a Python <c>set</c> or <c>frozenset</c>. A port of CPython 3.11's
    /// <c>setobject.c</c> hash table: elements sit where their CPython hashes (<see cref="PythonHash"/>)
    /// probe to, so iteration order matches CPython run with <c>PYTHONHASHSEED=0</c>.
    /// </summary>
    sealed class SourceSet : SourceObject
    {
        const string AddMethodName = "add";
        const string ClearMethodName = "clear";
        const string CopyMethodName = "copy";
        const string DifferenceMethodName = "difference";
        const string DifferenceUpdateMethodName = "difference_update";
        const string DiscardMethodName = "discard";
        const string IntersectionMethodName = "intersection";
        const string IntersectionUpdateMethodName = "intersection_update";
        const string IsDisjointMethodName = "isdisjoint";
        const string IsSubsetMethodName = "issubset";
        const string IsSupersetMethodName = "issuperset";
        const string PopMethodName = "pop";
        const string RemoveMethodName = "remove";
        const string SymmetricDifferenceMethodName = "symmetric_difference";
        const string SymmetricDifferenceUpdateMethodName = "symmetric_difference_update";
        const string UnionMethodName = "union";
        const string UpdateMethodName = "update";

        const string EmptyPopError = "pop from an empty set";

        // setobject.c constants
        const int MinSize = 8; // PySet_MINSIZE
        const int LinearProbes = 9; // LINEAR_PROBES
        const int PerturbShift = 5; // PERTURB_SHIFT
        const long DummyHash = -1; // a deleted slot's hash
        const long UncachedHash = -1;

        static readonly List<string> SetMethodNames = new List<string>
        {
            AddMethodName,
            ClearMethodName,
            CopyMethodName,
            DifferenceMethodName,
            DifferenceUpdateMethodName,
            DiscardMethodName,
            IntersectionMethodName,
            IntersectionUpdateMethodName,
            IsDisjointMethodName,
            IsSubsetMethodName,
            IsSupersetMethodName,
            PopMethodName,
            RemoveMethodName,
            SymmetricDifferenceMethodName,
            SymmetricDifferenceUpdateMethodName,
            UnionMethodName,
            UpdateMethodName
        };

        static readonly List<string> FrozenSetMethodNames = new List<string>
        {
            CopyMethodName,
            DifferenceMethodName,
            IntersectionMethodName,
            IsDisjointMethodName,
            IsSubsetMethodName,
            IsSupersetMethodName,
            SymmetricDifferenceMethodName,
            UnionMethodName
        };

        readonly bool _isFrozen;

        Entry[] _table = new Entry[MinSize];
        int _mask = MinSize - 1;
        int _fill; // active plus dummy slots
        int _used; // active slots
        int _finger; // where pop() starts looking
        long _frozenSetHash = UncachedHash;

        // Lazily created on first attribute access; most sets never look up a method.
        Dictionary<string, SourceValue> _methodCache;

        public SourceSet(bool isFrozen)
        {
            _isFrozen = isFrozen;
        }

        public override DataType Type => _isFrozen ? DataType.FrozenSet : DataType.Set;

        public override bool HasLength => true;

        public override int Length => _used;

        /// <summary>The last slot index; the table has <c>Mask + 1</c> slots.</summary>
        internal int Mask => _mask;

        /// <summary>CPython's <c>frozenset_hash</c>, cached after the first call.</summary>
        internal long FrozenSetHash
        {
            get
            {
                if (_frozenSetHash == UncachedHash)
                {
                    _frozenSetHash = ComputeFrozenSetHash();
                }

                return _frozenSetHash;
            }
        }

        /// <summary>
        /// CPython's <c>make_new_set</c>: a new set or frozenset holding the elements of
        /// <paramref name="iterable"/>.
        /// </summary>
        public static SourceSet FromIterable(SourceValue iterable, bool isFrozen)
        {
            var set = new SourceSet(isFrozen);
            set.UpdateInternal(iterable);
            return set;
        }

        /// <summary>CPython's <c>PyAnySet_Check</c>: a set or a frozenset.</summary>
        internal static bool IsAnySet(SourceValue value)
        {
            return value.DataType == DataType.Set || value.DataType == DataType.FrozenSet;
        }

        /// <summary>
        /// Computes and caches the frozenset hash now, so a frozenset shared through a compiled
        /// program's constant pool is never written to while it runs.
        /// </summary>
        internal void PrecomputeHash()
        {
            _ = FrozenSetHash;
        }

        /// <summary>Reads the slot at <paramref name="slot"/>. Returns <c>false</c> if it is empty or a dummy.</summary>
        internal bool TryGetActive(int slot, out SourceValue key)
        {
            ref var entry = ref _table[slot];
            key = entry.Key;
            return entry.State == EntryState.Active;
        }

        public void Add(SourceValue key)
        {
            PythonKeyComparer.ValidateHashable(key);
            AddEntry(key, PythonHash.Compute(key));
        }

        public override bool Contains(SourceValue value)
        {
            // set_contains: a set needle is looked up as the equal frozenset.
            if (value.DataType == DataType.Set)
            {
                value = new SourceValue(FromIterable(value, isFrozen: true));
            }

            PythonKeyComparer.ValidateHashable(value);
            return _table[LookKey(value, PythonHash.Compute(value))].State == EntryState.Active;
        }

        public override SourceIterator GetIterator()
        {
            return new SourceSetIterator(this);
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

        public override List<string> Directory => _isFrozen ? FrozenSetMethodNames : SetMethodNames;

        // set_richcompare's Py_EQ: a set and a frozenset with the same elements are equal.
        public override bool EqualsTo(SourceObject other)
        {
            if (!(other is SourceSet otherSet) || otherSet._used != _used)
            {
                return false;
            }

            if (_frozenSetHash != UncachedHash
                && otherSet._frozenSetHash != UncachedHash
                && _frozenSetHash != otherSet._frozenSetHash)
            {
                return false;
            }

            return IsSubsetOf(otherSet);
        }

        // Consistent with EqualsTo for host-side hashing; the hash doesn't depend on the table layout.
        public override int HashCode()
        {
            return (_isFrozen ? FrozenSetHash : ComputeFrozenSetHash()).GetHashCode();
        }

        public override string ToRepresentation()
        {
            var typeName = DataTypeNames.GetTypeName(Type);

            if (_used == 0)
            {
                return typeName + "()";
            }

            var sb = new StringBuilder();

            if (_isFrozen)
            {
                sb.Append(typeName).Append('(');
            }

            sb.Append('{');
            var isFirst = true;

            for (var i = 0; i <= _mask; i++)
            {
                ref var entry = ref _table[i];

                if (entry.State != EntryState.Active)
                {
                    continue;
                }

                if (!isFirst)
                {
                    sb.Append(", ");
                }

                entry.Key.AppendRepresentation(sb);
                isFirst = false;
            }

            sb.Append('}');

            if (_isFrozen)
            {
                sb.Append(')');
            }

            return sb.ToString();
        }

        public Func<SourceValue[], SourceValue> GetMethod(string methodName)
        {
            switch (methodName)
            {
                case AddMethodName:
                    return AddMethod;
                case ClearMethodName:
                    return ClearMethod;
                case CopyMethodName:
                    return CopyMethod;
                case DifferenceMethodName:
                    return DifferenceMethod;
                case DifferenceUpdateMethodName:
                    return DifferenceUpdateMethod;
                case DiscardMethodName:
                    return DiscardMethod;
                case IntersectionMethodName:
                    return IntersectionMethod;
                case IntersectionUpdateMethodName:
                    return IntersectionUpdateMethod;
                case IsDisjointMethodName:
                    return IsDisjointMethod;
                case IsSubsetMethodName:
                    return IsSubsetMethod;
                case IsSupersetMethodName:
                    return IsSupersetMethod;
                case PopMethodName:
                    return PopMethod;
                case RemoveMethodName:
                    return RemoveMethod;
                case SymmetricDifferenceMethodName:
                    return SymmetricDifferenceMethod;
                case SymmetricDifferenceUpdateMethodName:
                    return SymmetricDifferenceUpdateMethod;
                case UnionMethodName:
                    return UnionMethod;
                case UpdateMethodName:
                    return UpdateMethod;
            }

            throw new NotImplementedException($"Method '{methodName}' is not implemented for SourceSet");
        }

        /// <summary>CPython's <c>set_update_internal</c>.</summary>
        internal void UpdateInternal(SourceValue other)
        {
            switch (other.DataType)
            {
                case DataType.Set:
                case DataType.FrozenSet:
                    Merge((SourceSet)other.ToObject());
                    return;
                case DataType.Dict:
                    UpdateFromDict((SourceDict)other.ToObject());
                    return;
            }

            var iterator = IteratorFactory.GetIterator(other);

            while (iterator.TryMoveNext(out var key))
            {
                Add(key);
            }
        }

        /// <summary>CPython's <c>set_merge</c>.</summary>
        internal void Merge(SourceSet other)
        {
            if (ReferenceEquals(other, this) || other._used == 0)
            {
                return;
            }

            // One big resize at the start, rather than growing while inserting.
            if ((long)(_fill + other._used) * 5 >= (long)_mask * 3)
            {
                Resize((long)(_used + other._used) * 2);
            }

            // An empty table the same size as a dummy-free source can take its slots as they are.
            if (_fill == 0 && _mask == other._mask && other._fill == other._used)
            {
                Array.Copy(other._table, _table, other._table.Length);
                _fill = other._fill;
                _used = other._used;
                return;
            }

            // An empty table can't hold duplicates, so it can skip the equality checks.
            if (_fill == 0)
            {
                _fill = other._used;
                _used = other._used;

                foreach (var entry in other._table)
                {
                    if (entry.State == EntryState.Active)
                    {
                        InsertClean(_table, _mask, entry.Key, entry.Hash);
                    }
                }

                return;
            }

            foreach (var entry in other._table)
            {
                if (entry.State == EntryState.Active)
                {
                    AddEntry(entry.Key, entry.Hash);
                }
            }
        }

        void UpdateFromDict(SourceDict dict)
        {
            if ((long)(_fill + dict.Length) * 5 >= (long)_mask * 3)
            {
                Resize((long)(_used + dict.Length) * 2);
            }

            for (var i = 0; i < dict.EntryCount; i++)
            {
                if (dict.TryGetEntry(i, out var key, out _))
                {
                    AddEntry(key, PythonHash.Compute(key));
                }
            }
        }

        /// <summary>CPython's <c>set_copy</c>: a new set of this one's type, merged from it.</summary>
        internal SourceSet Copy()
        {
            var copy = new SourceSet(_isFrozen);
            copy.Merge(this);
            return copy;
        }

        /// <summary>CPython's <c>set_or</c>: the <c>|</c> operator.</summary>
        internal SourceSet Or(SourceSet other)
        {
            var result = Copy();

            if (!ReferenceEquals(other, this))
            {
                result.Merge(other);
            }

            return result;
        }

        /// <summary>CPython's <c>set_intersection</c>, which also serves the <c>&amp;</c> operator.</summary>
        internal SourceSet Intersection(SourceValue other)
        {
            if (ReferenceEquals(other.ToObject(), this))
            {
                return Copy();
            }

            var result = new SourceSet(_isFrozen);

            if (IsAnySet(other))
            {
                // Walk the smaller set (the other one on a tie), keeping its entries the larger has.
                var larger = this;
                var smaller = (SourceSet)other.ToObject();

                if (smaller._used > larger._used)
                {
                    larger = smaller;
                    smaller = this;
                }

                foreach (var entry in smaller._table)
                {
                    if (entry.State == EntryState.Active && larger.ContainsEntry(entry.Key, entry.Hash))
                    {
                        result.AddEntry(entry.Key, entry.Hash);
                    }
                }

                return result;
            }

            var iterator = IteratorFactory.GetIterator(other);

            while (iterator.TryMoveNext(out var key))
            {
                // Unlike `in`, a set element raises rather than being looked up as a frozenset.
                PythonKeyComparer.ValidateHashable(key);
                var hash = PythonHash.Compute(key);

                if (!ContainsEntry(key, hash))
                {
                    continue;
                }

                result.AddEntry(key, hash);

                // Nothing more can match once every element of this set has been found.
                if (result._used >= _used)
                {
                    break;
                }
            }

            return result;
        }

        /// <summary>CPython's <c>set_difference</c>, which also serves the <c>-</c> operator.</summary>
        internal SourceSet Difference(SourceValue other)
        {
            int otherSize;

            if (IsAnySet(other))
            {
                otherSize = ((SourceSet)other.ToObject())._used;
            }
            else if (other.DataType == DataType.Dict)
            {
                otherSize = ((SourceDict)other.ToObject()).Length;
            }
            else
            {
                return CopyAndDifference(other);
            }

            // When this set is much bigger, copying it and removing the other's elements is cheaper.
            if ((_used >> 2) > otherSize)
            {
                return CopyAndDifference(other);
            }

            var result = new SourceSet(_isFrozen);
            var otherDict = other.DataType == DataType.Dict ? (SourceDict)other.ToObject() : null;
            var otherSet = otherDict == null ? (SourceSet)other.ToObject() : null;

            foreach (var entry in _table)
            {
                if (entry.State != EntryState.Active)
                {
                    continue;
                }

                var isInOther = otherDict?.ContainsKey(entry.Key) ?? otherSet.ContainsEntry(entry.Key, entry.Hash);

                if (!isInOther)
                {
                    result.AddEntry(entry.Key, entry.Hash);
                }
            }

            return result;
        }

        // set_copy_and_difference
        SourceSet CopyAndDifference(SourceValue other)
        {
            var result = Copy();
            result.DifferenceUpdate(other);
            return result;
        }

        /// <summary>CPython's <c>set_difference_update_internal</c>.</summary>
        internal void DifferenceUpdate(SourceValue other)
        {
            if (ReferenceEquals(other.ToObject(), this))
            {
                ClearInternal();
                return;
            }

            if (IsAnySet(other))
            {
                var otherSet = (SourceSet)other.ToObject();

                // When the other set is more than 8 times larger, only the common elements need removing.
                if ((otherSet._used >> 3) > _used)
                {
                    otherSet = Intersection(other);
                }

                foreach (var entry in otherSet._table)
                {
                    if (entry.State == EntryState.Active)
                    {
                        DiscardEntry(entry.Key, entry.Hash);
                    }
                }
            }
            else
            {
                var iterator = IteratorFactory.GetIterator(other);

                while (iterator.TryMoveNext(out var key))
                {
                    // Unlike discard(), a set element raises rather than being removed as a frozenset.
                    PythonKeyComparer.ValidateHashable(key);
                    DiscardEntry(key, PythonHash.Compute(key));
                }
            }

            // Rebuild the table once more than a quarter of its slots are dummies.
            if (_fill - _used <= _mask / 4)
            {
                return;
            }

            Resize(_used > 50000 ? (long)_used * 2 : (long)_used * 4);
        }

        /// <summary>CPython's <c>set_symmetric_difference</c>, which also serves the <c>^</c> operator.</summary>
        internal SourceSet SymmetricDifference(SourceValue other)
        {
            var result = FromIterable(other, _isFrozen);
            result.SymmetricDifferenceUpdate(new SourceValue(this));
            return result;
        }

        /// <summary>CPython's <c>set_symmetric_difference_update</c>.</summary>
        internal void SymmetricDifferenceUpdate(SourceValue other)
        {
            if (ReferenceEquals(other.ToObject(), this))
            {
                ClearInternal();
                return;
            }

            // Each of the other's elements is removed if present, and added if not.
            if (other.DataType == DataType.Dict)
            {
                var dict = (SourceDict)other.ToObject();

                for (var i = 0; i < dict.EntryCount; i++)
                {
                    if (dict.TryGetEntry(i, out var key, out _))
                    {
                        var hash = PythonHash.Compute(key);

                        if (!DiscardEntry(key, hash))
                        {
                            AddEntry(key, hash);
                        }
                    }
                }

                return;
            }

            var otherSet = IsAnySet(other) ? (SourceSet)other.ToObject() : FromIterable(other, _isFrozen);

            foreach (var entry in otherSet._table)
            {
                if (entry.State == EntryState.Active && !DiscardEntry(entry.Key, entry.Hash))
                {
                    AddEntry(entry.Key, entry.Hash);
                }
            }
        }

        /// <summary>set_richcompare's ordering operators: subset and superset tests.</summary>
        internal bool Compare(SourceSet other, Operator op)
        {
            switch (op)
            {
                case Operator.LessEqual:
                    return IsSubsetOf(other);
                case Operator.GreaterEqual:
                    return other.IsSubsetOf(this);
                case Operator.Less:
                    return _used < other._used && IsSubsetOf(other);
                case Operator.Greater:
                    return _used > other._used && other.IsSubsetOf(this);
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }
        }

        // set_swap_bodies, keeping only what this set needs: the other set's table and counts. The
        // pop finger stays, as in CPython.
        void TakeTableOf(SourceSet other)
        {
            _table = other._table;
            _mask = other._mask;
            _fill = other._fill;
            _used = other._used;
        }

        // set_add_entry
        void AddEntry(SourceValue key, long hash)
        {
            var mask = (ulong)_mask;
            var slot = (ulong)hash & mask;
            var perturb = (ulong)hash;
            var freeSlot = -1;

            while (true)
            {
                var probe = slot;
                var probesLeft = slot + LinearProbes <= mask ? LinearProbes : 0;

                while (true)
                {
                    ref var entry = ref _table[probe];

                    if (entry.State == EntryState.Unused)
                    {
                        if (freeSlot >= 0)
                        {
                            // Reuse the last dummy slot passed on the way here.
                            _table[freeSlot] = new Entry(key, hash);
                            _used++;
                            return;
                        }

                        entry = new Entry(key, hash);
                        _fill++;
                        _used++;

                        if ((ulong)_fill * 5 >= mask * 3)
                        {
                            Resize(_used > 50000 ? (long)_used * 2 : (long)_used * 4);
                        }

                        return;
                    }

                    if (entry.State == EntryState.Active && entry.Hash == hash && PythonKeyComparer.AreEqual(entry.Key, key))
                    {
                        return;
                    }

                    if (entry.State == EntryState.Dummy)
                    {
                        freeSlot = (int)probe;
                    }

                    if (probesLeft == 0)
                    {
                        break;
                    }

                    probesLeft--;
                    probe++;
                }

                perturb >>= PerturbShift;
                slot = (slot * 5 + 1 + perturb) & mask;
            }
        }

        // set_lookkey: the slot holding the key, or the empty slot where the search ended.
        int LookKey(SourceValue key, long hash)
        {
            var mask = (ulong)_mask;
            var slot = (ulong)hash & mask;
            var perturb = (ulong)hash;

            while (true)
            {
                var probe = slot;
                var probesLeft = slot + LinearProbes <= mask ? LinearProbes : 0;

                while (true)
                {
                    ref var entry = ref _table[probe];

                    if (entry.State == EntryState.Unused)
                    {
                        return (int)probe;
                    }

                    if (entry.State == EntryState.Active && entry.Hash == hash && PythonKeyComparer.AreEqual(entry.Key, key))
                    {
                        return (int)probe;
                    }

                    if (probesLeft == 0)
                    {
                        break;
                    }

                    probesLeft--;
                    probe++;
                }

                perturb >>= PerturbShift;
                slot = (slot * 5 + 1 + perturb) & mask;
            }
        }

        // set_insert_clean: for a table known to have no dummies and no equal key.
        static void InsertClean(Entry[] table, int tableMask, SourceValue key, long hash)
        {
            var mask = (ulong)tableMask;
            var slot = (ulong)hash & mask;
            var perturb = (ulong)hash;

            while (true)
            {
                if (table[slot].State == EntryState.Unused)
                {
                    table[slot] = new Entry(key, hash);
                    return;
                }

                if (slot + LinearProbes <= mask)
                {
                    for (var j = 1UL; j <= LinearProbes; j++)
                    {
                        if (table[slot + j].State == EntryState.Unused)
                        {
                            table[slot + j] = new Entry(key, hash);
                            return;
                        }
                    }
                }

                perturb >>= PerturbShift;
                slot = (slot * 5 + 1 + perturb) & mask;
            }
        }

        // set_table_resize: rebuild into the smallest power-of-two table (at least 8) larger than minUsed.
        void Resize(long minUsed)
        {
            var newSize = (long)MinSize;

            while (newSize <= minUsed)
            {
                newSize <<= 1;
            }

            // A small table with no dummies has nothing to gain from a rebuild.
            if (newSize == MinSize && _table.Length == MinSize && _fill == _used)
            {
                return;
            }

            var oldTable = _table;
            _table = new Entry[newSize];
            _mask = (int)newSize - 1;
            _fill = _used;

            foreach (var entry in oldTable)
            {
                if (entry.State == EntryState.Active)
                {
                    InsertClean(_table, _mask, entry.Key, entry.Hash);
                }
            }
        }

        bool Discard(SourceValue key)
        {
            // set_discard: a set argument is removed as the equal frozenset.
            if (key.DataType == DataType.Set)
            {
                key = new SourceValue(FromIterable(key, isFrozen: true));
            }

            PythonKeyComparer.ValidateHashable(key);
            return DiscardEntry(key, PythonHash.Compute(key));
        }

        // set_discard_entry: true if the key was found and replaced by a dummy.
        bool DiscardEntry(SourceValue key, long hash)
        {
            var slot = LookKey(key, hash);

            if (_table[slot].State != EntryState.Active)
            {
                return false;
            }

            _table[slot] = Entry.Dummy;
            _used--;
            return true;
        }

        // set_clear_internal: back to an empty minimum-size table. The pop finger is left alone.
        void ClearInternal()
        {
            _table = new Entry[MinSize];
            _mask = MinSize - 1;
            _fill = 0;
            _used = 0;
        }

        bool IsSubsetOf(SourceSet other)
        {
            if (_used > other._used)
            {
                return false;
            }

            foreach (var entry in _table)
            {
                if (entry.State == EntryState.Active
                    && other._table[other.LookKey(entry.Key, entry.Hash)].State != EntryState.Active)
                {
                    return false;
                }
            }

            return true;
        }

        bool ContainsEntry(SourceValue key, long hash)
        {
            return _table[LookKey(key, hash)].State == EntryState.Active;
        }

        // frozenset_hash: an order-independent xor of every slot's shuffled hash, with the effect of
        // empty and dummy slots removed so only the elements count.
        long ComputeFrozenSetHash()
        {
            unchecked
            {
                ulong hash = 0;

                foreach (var entry in _table)
                {
                    hash ^= ShuffleBits((ulong)entry.Hash);
                }

                if (((_mask + 1 - _fill) & 1) != 0)
                {
                    hash ^= ShuffleBits(0);
                }

                if (((_fill - _used) & 1) != 0)
                {
                    hash ^= ShuffleBits((ulong)DummyHash);
                }

                hash ^= ((ulong)_used + 1) * 1927868237UL;
                hash ^= (hash >> 11) ^ (hash >> 25);
                hash = hash * 69069U + 907133923UL;

                if (hash == ulong.MaxValue)
                {
                    hash = 590923713UL;
                }

                return (long)hash;
            }
        }

        static ulong ShuffleBits(ulong hash)
        {
            unchecked
            {
                return ((hash ^ 89869747UL) ^ (hash << 16)) * 3644798167UL;
            }
        }

        SourceValue AddMethod(SourceValue[] args)
        {
            ValidateArgumentCount(args, 1);
            Add(args[0]);
            return SourceValue.None;
        }

        SourceValue ClearMethod(SourceValue[] args)
        {
            ValidateArgumentCount(args, 0);
            ClearInternal();
            return SourceValue.None;
        }

        // set.copy builds a new set; frozenset.copy returns the frozenset itself, as CPython does.
        SourceValue CopyMethod(SourceValue[] args)
        {
            ValidateArgumentCount(args, 0);

            return new SourceValue(_isFrozen ? this : Copy());
        }

        SourceValue DiscardMethod(SourceValue[] args)
        {
            ValidateArgumentCount(args, 1);
            Discard(args[0]);
            return SourceValue.None;
        }

        SourceValue RemoveMethod(SourceValue[] args)
        {
            ValidateArgumentCount(args, 1);

            if (!Discard(args[0]))
            {
                throw new SubscriptException(args[0].ToRepresentation());
            }

            return SourceValue.None;
        }

        // set_pop: takes the first active slot at or after the finger, wrapping around.
        SourceValue PopMethod(SourceValue[] args)
        {
            ValidateArgumentCount(args, 0);

            if (_used == 0)
            {
                throw new SubscriptException(StringRepresentation.Format(EmptyPopError));
            }

            var slot = _finger & _mask;

            while (_table[slot].State != EntryState.Active)
            {
                slot++;

                if (slot > _mask)
                {
                    slot = 0;
                }
            }

            var key = _table[slot].Key;
            _table[slot] = Entry.Dummy;
            _used--;
            _finger = slot + 1;
            return key;
        }

        SourceValue UpdateMethod(SourceValue[] args)
        {
            foreach (var other in args ?? Array.Empty<SourceValue>())
            {
                UpdateInternal(other);
            }

            return SourceValue.None;
        }

        // set_union: unlike frozenset.copy(), always a new set, even with no arguments.
        SourceValue UnionMethod(SourceValue[] args)
        {
            var result = Copy();

            foreach (var other in args ?? Array.Empty<SourceValue>())
            {
                if (!ReferenceEquals(other.ToObject(), this))
                {
                    result.UpdateInternal(other);
                }
            }

            return new SourceValue(result);
        }

        // set_intersection_multi: intersects with each argument in turn.
        SourceValue IntersectionMethod(SourceValue[] args)
        {
            return new SourceValue(IntersectionOfAll(args));
        }

        SourceSet IntersectionOfAll(SourceValue[] args)
        {
            if (args == null || args.Length == 0)
            {
                return Copy();
            }

            var result = this;

            foreach (var other in args)
            {
                result = result.Intersection(other);
            }

            return result;
        }

        // set_intersection_update_multi: this set takes over the intersection's table.
        SourceValue IntersectionUpdateMethod(SourceValue[] args)
        {
            TakeTableOf(IntersectionOfAll(args));
            return SourceValue.None;
        }

        // set_difference_multi: the first argument through set_difference, the rest as updates.
        SourceValue DifferenceMethod(SourceValue[] args)
        {
            if (args == null || args.Length == 0)
            {
                return new SourceValue(Copy());
            }

            var result = Difference(args[0]);

            for (var i = 1; i < args.Length; i++)
            {
                result.DifferenceUpdate(args[i]);
            }

            return new SourceValue(result);
        }

        SourceValue DifferenceUpdateMethod(SourceValue[] args)
        {
            foreach (var other in args ?? Array.Empty<SourceValue>())
            {
                DifferenceUpdate(other);
            }

            return SourceValue.None;
        }

        SourceValue SymmetricDifferenceMethod(SourceValue[] args)
        {
            ValidateArgumentCount(args, 1);
            return new SourceValue(SymmetricDifference(args[0]));
        }

        SourceValue SymmetricDifferenceUpdateMethod(SourceValue[] args)
        {
            ValidateArgumentCount(args, 1);
            SymmetricDifferenceUpdate(args[0]);
            return SourceValue.None;
        }

        // set_isdisjoint: iterate the smaller set and look each element up in the larger.
        SourceValue IsDisjointMethod(SourceValue[] args)
        {
            ValidateArgumentCount(args, 1);
            var other = args[0];

            if (ReferenceEquals(other.ToObject(), this))
            {
                return new SourceValue(_used == 0);
            }

            if (other.DataType == DataType.Set || other.DataType == DataType.FrozenSet)
            {
                var larger = this;
                var smaller = (SourceSet)other.ToObject();

                if (smaller._used > larger._used)
                {
                    var swap = larger;
                    larger = smaller;
                    smaller = swap;
                }

                foreach (var entry in smaller._table)
                {
                    if (entry.State == EntryState.Active && larger.ContainsEntry(entry.Key, entry.Hash))
                    {
                        return new SourceValue(false);
                    }
                }

                return new SourceValue(true);
            }

            var iterator = IteratorFactory.GetIterator(other);

            while (iterator.TryMoveNext(out var key))
            {
                if (ContainsHashable(key))
                {
                    return new SourceValue(false);
                }
            }

            return new SourceValue(true);
        }

        // set_issubset: a non-set argument is first collected into a set.
        SourceValue IsSubsetMethod(SourceValue[] args)
        {
            ValidateArgumentCount(args, 1);
            var other = args[0];

            var otherSet = other.DataType == DataType.Set || other.DataType == DataType.FrozenSet
                ? (SourceSet)other.ToObject()
                : FromIterable(other, isFrozen: false);

            return new SourceValue(IsSubsetOf(otherSet));
        }

        // set_issuperset: a set argument checks the reverse subset; anything else is iterated.
        SourceValue IsSupersetMethod(SourceValue[] args)
        {
            ValidateArgumentCount(args, 1);
            var other = args[0];

            if (other.DataType == DataType.Set || other.DataType == DataType.FrozenSet)
            {
                return new SourceValue(((SourceSet)other.ToObject()).IsSubsetOf(this));
            }

            var iterator = IteratorFactory.GetIterator(other);

            while (iterator.TryMoveNext(out var key))
            {
                if (!ContainsHashable(key))
                {
                    return new SourceValue(false);
                }
            }

            return new SourceValue(true);
        }

        // set_contains_key: unlike `in`, a set element raises rather than being converted.
        bool ContainsHashable(SourceValue key)
        {
            PythonKeyComparer.ValidateHashable(key);
            return ContainsEntry(key, PythonHash.Compute(key));
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

        enum EntryState : byte
        {
            Unused,
            Active,
            Dummy
        }

        // setentry: an empty slot has hash 0, a dummy (deleted) slot hash -1, as frozenset_hash expects.
        struct Entry
        {
            public static readonly Entry Dummy = new Entry { Hash = DummyHash, State = EntryState.Dummy };

            public SourceValue Key;
            public long Hash;
            public EntryState State;

            public Entry(SourceValue key, long hash)
            {
                Key = key;
                Hash = hash;
                State = EntryState.Active;
            }
        }
    }
}
