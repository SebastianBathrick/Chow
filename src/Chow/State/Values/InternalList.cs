using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Interpreter.State.Values
{
    internal class InternalList
    {
        const string METHOD_APPEND_NAME = "append";
        const string METHOD_CLEAR_NAME = "clear";
        const string METHOD_INSERT_NAME = "insert";
        const string METHOD_POP_NAME = "pop";
        const string METHOD_REMOVE_NAME = "remove";
        const string METHOD_REVERSE_NAME = "reverse";


        List<TaggedUnion> _elements;

        public int Count => _elements.Count;

        public TaggedUnion this[int index]
        {
            get
            {
                int idx = NormalizeIndex(index);
                return _elements[idx];
            }
            set
            {
                int idx = NormalizeIndex(index);
                _elements[idx] = value;
            }
        }

        int NormalizeIndex(int index)
        {
            int idx = index < 0 ? _elements.Count + index : index;
            if (idx < 0 || idx >= _elements.Count)
            {
                throw new IndexOutOfRangeException();
            }
            return idx;
        }

        public TaggedUnion this[string name]
        {
            get
            {
                // Will throw if method name is invalid, which is the expected behavior
                return new TaggedUnion(GetMethod(name));
            }
        }

        public InternalList()
        {
            _elements = new List<TaggedUnion>();
        }

        TaggedUnion Append(TaggedUnion[] args)
        {
            ValidateArguments(args, 1);
            _elements.Add(args[0]);
            return TaggedUnion.None;
        }

        TaggedUnion Clear(TaggedUnion[] args)
        {
            ValidateArguments(args, 0);
            _elements.Clear();
            return TaggedUnion.None;
        }

        TaggedUnion Insert(TaggedUnion[] args)
        {
            ValidateArguments(args, 2);

            if (args[0].Tag != Tag.Int)
            {
                throw new ArgumentException($"Argument 0 must be of type {Tag.Int}, but was {args[0].Tag}");
            }

            int idx = (int)args[0].IntegerValue;

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

            int index = _elements.Count - 1;
            if (args != null && args.Length == 1)
            {
                if (args[0].Tag != Tag.Int)
                {
                    throw new ArgumentException($"Argument 0 must be of type {Tag.Int}, but was {args[0].Tag}");
                }
                index = (int)args[0].IntegerValue;
                if (index < 0)
                {
                    index = _elements.Count + index;
                }
                if (index < 0 || index >= _elements.Count)
                {
                    throw new IndexOutOfRangeException();
                }
            }

            TaggedUnion value = _elements[index];
            _elements.RemoveAt(index);

            return value;
        }

        TaggedUnion Remove(TaggedUnion[] args)
        {
            ValidateArguments(args, 1);
            for (int i = 0; i < _elements.Count; i++)
            {
                if (_elements[i] == args[0])
                {
                    _elements.RemoveAt(i);
                    return TaggedUnion.None;
                }
            }
            throw new ArgumentException("list.remove(x): x not in list");
        }

        TaggedUnion Reverse(TaggedUnion[] args)
        {
            ValidateArguments(args, 0);
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
            for (int i = 0; i < a._elements.Count; i++)
            {
                if (a._elements[i] != b._elements[i])
                {
                    return false;
                }
            }
            return true;
        }

        public static InternalList Concat(InternalList a, InternalList b)
        {
            InternalList result = new InternalList();
            result._elements.AddRange(a._elements);
            result._elements.AddRange(b._elements);
            return result;
        }

        public static InternalList Repeat(InternalList a, int n)
        {
            InternalList result = new InternalList();
            if (n <= 0)
            {
                return result;
            }
            for (int i = 0; i < n; i++)
            {
                result._elements.AddRange(a._elements);
            }
            return result;
        }

        // FUTURE: strings will need a parallel GetSlice returning a string, not a list. Do not abstract.
        public TaggedUnion GetSlice(TaggedUnion startUnion, TaggedUnion stopUnion, TaggedUnion stepUnion)
        {
            int length = _elements.Count;

            int step = SliceArgOrDefault(stepUnion, 1);
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
            if (startUnion.Tag == Tag.None)
            {
                start = step < 0 ? upper : lower;
            }
            else
            {
                start = SliceArgOrDefault(startUnion, 0);
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
            if (stopUnion.Tag == Tag.None)
            {
                stop = step < 0 ? lower : upper;
            }
            else
            {
                stop = SliceArgOrDefault(stopUnion, 0);
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

            InternalList sliced = new InternalList();
            if (step > 0)
            {
                for (int i = start; i < stop; i += step)
                {
                    sliced._elements.Add(_elements[i]);
                }
            }
            else
            {
                for (int i = start; i > stop; i += step)
                {
                    sliced._elements.Add(_elements[i]);
                }
            }
            return new TaggedUnion(sliced);
        }

        static int SliceArgOrDefault(TaggedUnion union, int defaultValue)
        {
            if (union.Tag == Tag.None)
            {
                return defaultValue;
            }
            if (union.Tag != Tag.Int)
            {
                throw new ArgumentException($"slice indices must be integers or None, got {union.Tag}");
            }
            return (int)union.IntegerValue;
        }

        public override string ToString()
        {
            // FUTURE: once dicts/class instances exist, Repr below will grow branches for them.
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            for (int i = 0; i < _elements.Count; i++)
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
            switch (value.Tag)
            {
                case Tag.None:
                    sb.Append("None");
                    return;
                case Tag.Boolean:
                    sb.Append(value.BooleanValue ? "True" : "False");
                    return;
                case Tag.Int:
                    sb.Append(value.IntegerValue);
                    return;
                case Tag.Float:
                    // Python prints `1.0`, not `1`. C#'s default float.ToString() may drop the trailing zero.
                    double f = value.FloatValue;
                    string fs = f.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                    if (fs.IndexOfAny(new[] { '.', 'e', 'E', 'n', 'N', 'i', 'I' }) < 0)
                    {
                        fs += ".0";
                    }
                    sb.Append(fs);
                    return;
                case Tag.Str:
                    sb.Append('\'');
                    sb.Append(value.StringValue);
                    sb.Append('\'');
                    return;
                case Tag.List:
                    sb.Append(value.ListValue.ToString());
                    return;
                case Tag.Dict:
                    sb.Append(value.DictValue.ToString());
                    return;
                default:
                    sb.Append(value.ToString());
                    return;
            }
        }

        static void ValidateArguments(TaggedUnion[] args, int reqArgCount = 0, Tag[] reqTypes = null)
        {
            int expectedCount = reqTypes?.Length ?? reqArgCount;
            int actualCount = args?.Length ?? 0;

            if (actualCount != expectedCount)
            {
                throw new ArgumentException($"Method requires {expectedCount} arguments, but {actualCount} were provided");
            }

            if (reqTypes == null)
            {
                return;
            }

            for (int i = 0; i < reqTypes.Length; i++)
            {
                if (args[i].Tag == reqTypes[i])
                {
                    continue;
                }

                throw new ArgumentException($"Argument {i} must be of type {reqTypes[i]}, but was {args[i].Tag}");
            }
        }
    }
}
