using System;
using System.Collections.Generic;
using Chow.Code;
using Chow.Interpreter.Exceptions;

namespace Chow.SourceData
{
    /// <summary>
    /// Live view of a dict's keys. (Python: <c>dict_keys</c>) Reflects later changes to the dict
    /// and behaves like a set: it compares with sets and combines with any iterable through
    /// <c>| &amp; - ^</c>, as ported from CPython 3.11's <c>dictobject.c</c>.
    /// </summary>
    sealed class SourceDictKeys : SourceObject
    {
        const string ReprFormat = "dict_keys({0})";
        const string IsDisjointMethodName = "isdisjoint";

        static readonly List<string> MethodNames = new List<string> { IsDisjointMethodName };

        readonly SourceDict _dict;

        // Lazily created on first attribute access; most views never look up a method.
        Dictionary<string, SourceValue> _methodCache;

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

        public Func<SourceValue[], SourceValue> GetMethod(string methodName)
        {
            if (methodName == IsDisjointMethodName)
            {
                return IsDisjointMethod;
            }

            throw new NotImplementedException($"Method '{methodName}' is not implemented for SourceDictKeys");
        }

        #region Set Operations

        // The operators run with a keys view on either side and any iterable on the other, so they
        // take both operands as values. `self` is the left operand, which needn't be the view.

        /// <summary>CPython's <c>dictviews_or</c>.</summary>
        internal static SourceSet Or(SourceValue self, SourceValue other)
        {
            var result = ToSet(self);
            result.UpdateInternal(other);
            return result;
        }

        /// <summary>CPython's <c>dictviews_sub</c>.</summary>
        internal static SourceSet Subtract(SourceValue self, SourceValue other)
        {
            var result = ToSet(self);
            result.DifferenceUpdate(other);
            return result;
        }

        /// <summary>CPython's <c>dictviews_xor</c>.</summary>
        internal static SourceSet Xor(SourceValue self, SourceValue other)
        {
            var result = ToSet(self);
            result.SymmetricDifferenceUpdate(other);
            return result;
        }

        /// <summary>CPython's <c>_PyDictView_Intersect</c>.</summary>
        internal static SourceSet And(SourceValue self, SourceValue other)
        {
            // The interpreter swaps the operands when the view is on the right of &.
            if (self.DataType != DataType.DictKeys)
            {
                var swap = self;
                self = other;
                other = swap;
            }

            var length = ((SourceDictKeys)self.ToObject()).Length;

            // A set at least as big as the view does the work: other.intersection(self).
            if (other.DataType == DataType.Set && length <= ((SourceSet)other.ToObject()).Length)
            {
                return ((SourceSet)other.ToObject()).Intersection(self);
            }

            // Of two views, iterate the smaller.
            if (other.DataType == DataType.DictKeys && ((SourceDictKeys)other.ToObject()).Length > length)
            {
                var swap = self;
                self = other;
                other = swap;
            }

            var keys = (SourceDictKeys)self.ToObject();
            var result = new SourceSet(isFrozen: false);
            var iterator = IteratorFactory.GetIterator(other);

            while (iterator.TryMoveNext(out var key))
            {
                if (keys.Contains(key))
                {
                    result.Add(key);
                }
            }

            return result;
        }

        /// <summary>
        /// CPython's <c>dictview_richcompare</c> for a view on the left and a set, frozenset, or keys
        /// view on the right.
        /// </summary>
        internal static bool Compare(SourceValue self, SourceValue other, Operator op)
        {
            var selfLength = self.ToISourceObject().Length;
            var otherLength = other.ToISourceObject().Length;

            switch (op)
            {
                case Operator.Equal:
                    return selfLength == otherLength && AllContainedIn(self, other);
                case Operator.Less:
                    return selfLength < otherLength && AllContainedIn(self, other);
                case Operator.LessEqual:
                    return selfLength <= otherLength && AllContainedIn(self, other);
                case Operator.Greater:
                    return selfLength > otherLength && AllContainedIn(other, self);
                case Operator.GreaterEqual:
                    return selfLength >= otherLength && AllContainedIn(other, self);
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }
        }

        /// <summary>Whether a value can be compared with a keys view: a set, frozenset, or keys view.</summary>
        internal static bool IsComparableWith(SourceValue value)
        {
            return SourceSet.IsAnySet(value) || value.DataType == DataType.DictKeys;
        }

        // dictviews_to_set: a set of the value's elements. A keys view is read through its dict, which
        // takes the presizing dict path of set_update_internal.
        static SourceSet ToSet(SourceValue value)
        {
            if (value.DataType == DataType.DictKeys)
            {
                value = new SourceValue(((SourceDictKeys)value.ToObject())._dict);
            }

            return SourceSet.FromIterable(value, isFrozen: false);
        }

        static bool AllContainedIn(SourceValue self, SourceValue other)
        {
            var container = other.ToISourceObject();
            var iterator = IteratorFactory.GetIterator(self);

            while (iterator.TryMoveNext(out var item))
            {
                if (!container.Contains(item))
                {
                    return false;
                }
            }

            return true;
        }

        // dictviews_isdisjoint: against a set or view, iterate whichever is shorter.
        SourceValue IsDisjointMethod(SourceValue[] args)
        {
            ValidateArgumentCount(args, 1);
            var other = args[0];

            if (ReferenceEquals(other.ToObject(), this))
            {
                return new SourceValue(Length == 0);
            }

            var self = new SourceValue(this);

            if (IsComparableWith(other) && other.ToISourceObject().Length > Length)
            {
                var swap = self;
                self = other;
                other = swap;
            }

            var container = self.ToISourceObject();
            var iterator = IteratorFactory.GetIterator(other);

            while (iterator.TryMoveNext(out var item))
            {
                if (container.Contains(item))
                {
                    return new SourceValue(false);
                }
            }

            return new SourceValue(true);
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

        #endregion
    }
}
