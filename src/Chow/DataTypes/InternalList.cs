using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
namespace Chow.DataTypes
{
    class InternalList
    {
        const string METHOD_APPEND_NAME = "append";
        const string METHOD_CLEAR_NAME = "clear";
        const string METHOD_INSERT_NAME = "insert";
        const string METHOD_POP_NAME = "pop";
        const string METHOD_REMOVE_NAME = "remove";
        const string METHOD_REVERSE_NAME = "reverse";


        readonly List<TaggedUnion> _elements = new List<TaggedUnion>();

        public int Count => _elements.Count;

        public TaggedUnion this[int index]
        {
            get
            {
                var idx = NormalizeIndex(index);
                return _elements[idx];
            }
            set
            {
                var idx = NormalizeIndex(index);
                _elements[idx] = value;
            }
        }

        int NormalizeIndex(int index)
        {
            var idx = index < 0 ? _elements.Count + index : index;

            if (idx < 0 || idx >= _elements.Count)
            {
                throw new IndexOutOfRangeException();
            }

            return idx;
        }

        // Will throw if method name is invalid, which is the expected behavior
        public TaggedUnion this[string name] => new TaggedUnion(GetMethod(name));

        TaggedUnion Append(TaggedUnion[] args)
        {
            ValidateArguments(args, 1);
            _elements.Add(args[0]);
            return TaggedUnion.None;
        }

        TaggedUnion Clear(TaggedUnion[] args)
        {
            ValidateArguments(args);
            _elements.Clear();
            return TaggedUnion.None;
        }

        TaggedUnion Insert(TaggedUnion[] args)
        {
            ValidateArguments(args, 2);

            if (args[0].Type != Tag.Long)
            {
                throw new ArgumentException($"Argument 0 must be of type {Tag.Long}, but was {args[0].Type}");
            }

            var idx = (int)args[0].AsType<long>();

            if (idx < 0)
            {
                idx = Math.Max(0, _elements.Count + idx);
            }
            else if (idx > _elements.Count)
            {
                idx = _elements.Count;
            }

            _elements.Insert(idx, args[1]);
            return TaggedUnion.None;
        }

        TaggedUnion Pop(TaggedUnion[] args)
        {
            if (args != null && args.Length > 1)
            {
                throw new ArgumentException($"Method 'pop' takes at most 1 argument, but {args.Length} were provided");
            }

            if (_elements.Count == 0)
            {
                throw new InvalidOperationException("pop from empty list");
            }

            var index = _elements.Count - 1;

            if (args != null && args.Length == 1)
            {
                if (args[0].Type != Tag.Long)
                {
                    throw new ArgumentException($"Argument 0 must be of type {Tag.Long}, but was {args[0].Type}");
                }

                index = (int)args[0].AsType<long>();

                if (index < 0)
                {
                    index = _elements.Count + index;
                }

                if (index < 0 || index >= _elements.Count)
                {
                    throw new IndexOutOfRangeException();
                }
            }

            var value = _elements[index];
            _elements.RemoveAt(index);

            return value;
        }

        TaggedUnion Remove(TaggedUnion[] args)
        {
            ValidateArguments(args, 1);

            for (var i = 0; i < _elements.Count; i++)
            {
                if (!_elements[i].IsTypeAgnosticEqualTo(args[0]))
                {
                    continue;
                }

                _elements.RemoveAt(i);
                return TaggedUnion.None;
            }

            throw new ArgumentException("list.remove(x): x not in list");
        }

        TaggedUnion Reverse(TaggedUnion[] args)
        {
            ValidateArguments(args);
            _elements.Reverse();
            return TaggedUnion.None;
        }

        // TODO: Refactor to reduce code duplication (e.g. with a dictionary of method name to Func<TaggedUnion[], TaggedUnion>)
        public TaggedUnion CallMethod(string methodName, TaggedUnion[] args = null)
        {
            switch (methodName)
            {
                case METHOD_APPEND_NAME:
                    return Append(args);

                case METHOD_CLEAR_NAME:
                    return Clear(args);

                case METHOD_INSERT_NAME:
                    return Insert(args);

                case METHOD_POP_NAME:
                    return Pop(args);

                case METHOD_REMOVE_NAME:
                    return Remove(args);

                case METHOD_REVERSE_NAME:
                    return Reverse(args);

                default:
                    throw new NotImplementedException($"Method '{methodName}' is not implemented for InternalList");
            }
        }

        public Func<TaggedUnion[], TaggedUnion> GetMethod(string methodName)
        {
            switch (methodName)
            {
                case METHOD_APPEND_NAME:
                    return Append;
                case METHOD_CLEAR_NAME:
                    return Clear;
                case METHOD_INSERT_NAME:
                    return Insert;
                case METHOD_POP_NAME:
                    return Pop;
                case METHOD_REMOVE_NAME:
                    return Remove;
                case METHOD_REVERSE_NAME:
                    return Reverse;
                default:
                    throw new NotImplementedException($"Method '{methodName}' is not implemented for InternalList");
            }
        }

        public void Add(TaggedUnion element)
        {
            _elements.Add(element);
        }

        public bool HasMethod(string methodName)
        {
            switch (methodName)
            {
                case METHOD_APPEND_NAME:
                case METHOD_CLEAR_NAME:
                case METHOD_INSERT_NAME:
                case METHOD_POP_NAME:
                case METHOD_REMOVE_NAME:
                case METHOD_REVERSE_NAME:
                    return true;
                default:
                    return false;
            }
        }

        public static bool ElementsEqual(InternalList a, InternalList b)
        {
            if (a._elements.Count != b._elements.Count)
            {
                return false;
            }

            for (var i = 0; i < a._elements.Count; i++)
            {
                if (!a._elements[i].IsTypeAgnosticEqualTo(b._elements[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static int ElementsHashCode(InternalList a)
        {
            // Order-sensitive polynomial combine — paired with ElementsEqual's order-sensitive check.
            unchecked
            {
                var hash = 17;

                foreach (var element in a._elements)
                {
                    hash = hash * 31 + element.GetHashCode();
                }

                return hash;
            }
        }

        public static InternalList Concat(InternalList a, InternalList b)
        {
            var result = new InternalList();
            result._elements.AddRange(a._elements);
            result._elements.AddRange(b._elements);
            return result;
        }

        public static InternalList Repeat(InternalList a, int n)
        {
            var result = new InternalList();

            if (n <= 0)
            {
                return result;
            }

            for (var i = 0; i < n; i++)
            {
                result._elements.AddRange(a._elements);
            }

            return result;
        }

        // FUTURE: strings will need a parallel GetSlice returning a string, not a list. Do not abstract.
        public TaggedUnion GetSlice(TaggedUnion startValue, TaggedUnion stopValue, TaggedUnion stepValue)
        {
            var length = _elements.Count;

            var step = SliceArgOrDefault(stepValue, 1);

            if (step == 0)
            {
                throw new ArgumentException("slice step cannot be zero");
            }

            int lower;
            int upper;

            if (step > 0)
            {
                lower = 0;
                upper = length;
            }
            else
            {
                lower = -1;
                upper = length - 1;
            }

            int start;

            if (startValue.Type == Tag.None)
            {
                start = step < 0 ? upper : lower;
            }
            else
            {
                start = SliceArgOrDefault(startValue, 0);

                if (start < 0)
                {
                    start += length;
                }

                if (start < lower)
                {
                    start = lower;
                }

                if (start > upper)
                {
                    start = upper;
                }
            }

            int stop;

            if (stopValue.Type == Tag.None)
            {
                stop = step < 0 ? lower : upper;
            }
            else
            {
                stop = SliceArgOrDefault(stopValue, 0);

                if (stop < 0)
                {
                    stop += length;
                }

                if (stop < lower)
                {
                    stop = lower;
                }

                if (stop > upper)
                {
                    stop = upper;
                }
            }

            var sliced = new InternalList();

            if (step > 0)
            {
                for (var i = start; i < stop; i += step)
                {
                    sliced._elements.Add(_elements[i]);
                }
            }
            else
            {
                for (var i = start; i > stop; i += step)
                {
                    sliced._elements.Add(_elements[i]);
                }
            }

            return new TaggedUnion(sliced);
        }

        static int SliceArgOrDefault(TaggedUnion value, int defaultValue)
        {
            if (value.Type == Tag.None)
            {
                return defaultValue;
            }

            if (value.Type != Tag.Long)
            {
                throw new ArgumentException($"slice indices must be integers or None, got {value.Type}");
            }

            return (int)value.AsType<long>();
        }

        public override string ToString()
        {
            // FUTURE: once dicts/class instances exist, Repr below will grow branches for them.
            var sb = new StringBuilder();
            sb.Append('[');

            for (var i = 0; i < _elements.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                Repr(sb, _elements[i]);
            }

            sb.Append(']');
            return sb.ToString();
        }

        // Python-faithful repr used inside collection contexts: strings get single quotes here, even though
        // a standalone string prints without quotes via ChowStr.ToString.
        static void Repr(StringBuilder sb, TaggedUnion value)
        {
            switch (value.Type)
            {
                case Tag.None:
                    sb.Append("None");
                    return;
                case Tag.Bool:
                    sb.Append(value.AsType<bool>() ? "True" : "False");
                    return;
                case Tag.Long:
                    sb.Append(value.AsType<long>());
                    return;
                case Tag.Double:
                    // Python prints `1.0`, not `1`. C#'s default float.ToString() may drop the trailing zero.
                    var f = value.AsType<double>();
                    var fs = f.ToString("R", CultureInfo.InvariantCulture);

                    if (fs.IndexOfAny(new[] { '.', 'e', 'E', 'n', 'N', 'i', 'I' }) < 0)
                    {
                        fs += ".0";
                    }

                    sb.Append(fs);
                    return;
                case Tag.Str:
                    sb.Append('\'');
                    sb.Append(value.AsType<string>());
                    sb.Append('\'');
                    return;
                case Tag.List:
                    sb.Append(value.AsType<InternalList>());
                    return;
                case Tag.Dict:
                    sb.Append(value.AsType<InternalDict>());
                    return;
                case Tag.Object:
                case Tag.Range:
                default:
                    sb.Append(value.ToString());
                    return;
            }
        }

        static void ValidateArguments(TaggedUnion[] args, int reqArgCount = 0, Tag[] reqTypes = null)
        {
            var expectedCount = reqTypes?.Length ?? reqArgCount;
            var actualCount = args?.Length ?? 0;

            if (actualCount != expectedCount)
            {
                throw new ArgumentException($"Method requires {expectedCount} arguments, but {actualCount} were provided");
            }

            if (reqTypes == null)
            {
                return;
            }
            
            for (var i = 0; i < reqTypes.Length; i++)
            {
                if (args[i].Type == reqTypes[i])
                {
                    continue;
                }

                throw new ArgumentException($"Argument {i} must be of type {reqTypes[i]}, but was {args[i].Type}");
            }
        }
    }
}
