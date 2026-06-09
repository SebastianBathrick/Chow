using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Chow.VM;
namespace Chow.SourceData
{
    class SourceList : SourceObject
    {
        const string METHOD_APPEND_NAME = "append";
        const string METHOD_CLEAR_NAME = "clear";
        const string METHOD_INSERT_NAME = "insert";
        const string METHOD_POP_NAME = "pop";
        const string METHOD_REMOVE_NAME = "remove";
        const string METHOD_REVERSE_NAME = "reverse";

        const string INDEX_TYPE_ERROR_FORMAT = "list indices must be integers, not {0}";

        readonly List<SourceValue> _elements = new List<SourceValue>();

        public int Count => _elements.Count;

        public override DataType Type => DataType.List;

        public override bool HasLength => true;

        public override int Length => _elements.Count;

        public override SourceValue GetItem(SourceValue key)
        {
            if (key.DataType == DataType.Long)
            {
                return this[(int)key.ToLong()];
            }

            if (key.DataType == DataType.Slice)
            {
                var slice = (SourceSlice)key.ToSourceObject();
                return GetSlice(slice.Start, slice.Stop, slice.Step);
            }

            throw new DataTypeException(string.Format(INDEX_TYPE_ERROR_FORMAT, key.DataType));
        }

        public override void SetItem(SourceValue key, SourceValue value)
        {
            if (key.DataType != DataType.Long)
            {
                throw new DataTypeException(string.Format(INDEX_TYPE_ERROR_FORMAT, key.DataType));
            }

            this[(int)key.ToLong()] = value;
        }

        public override void Append(SourceValue value)
        {
            _elements.Add(value);
        }

        public override SourceValue GetAttribute(SourceValue name)
        {
            return new SourceValue(GetMethod(name.ToString()));
        }

        public override List<string> Directory => new List<string>
        {
            METHOD_APPEND_NAME,
            METHOD_CLEAR_NAME,
            METHOD_INSERT_NAME,
            METHOD_POP_NAME,
            METHOD_REMOVE_NAME,
            METHOD_REVERSE_NAME
        };

        // Python `in` compares with == semantics, not strict identity/equality.
        public override bool Contains(SourceValue value)
        {
            for (var i = 0; i < _elements.Count; i++)
            {
                if (SourceValue.EvaluateEqual(r: value, l: _elements[i]).ToBool())
                {
                    return true;
                }
            }

            return false;
        }

        public override IIterator GetIterator()
        {
            return new SourceListIterator(this);
        }

        public override bool EqualsTo(SourceObject other)
        {
            return other is SourceList list && ElementsEqual(this, list);
        }

        public SourceValue this[int index]
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

        SourceValue Append(SourceValue[] args)
        {
            ValidateArguments(args, 1);
            _elements.Add(args[0]);
            return SourceValue.None;
        }

        SourceValue Clear(SourceValue[] args)
        {
            ValidateArguments(args);
            _elements.Clear();
            return SourceValue.None;
        }

        SourceValue Insert(SourceValue[] args)
        {
            ValidateArguments(args, 2);

            if (args[0].DataType != DataType.Long)
            {
                throw new ArgumentException($"Argument 0 must be of type {DataType.Long}, but was {args[0].DataType}");
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
            return SourceValue.None;
        }

        SourceValue Pop(SourceValue[] args)
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
                if (args[0].DataType != DataType.Long)
                {
                    throw new ArgumentException($"Argument 0 must be of type {DataType.Long}, but was {args[0].DataType}");
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

        SourceValue Remove(SourceValue[] args)
        {
            ValidateArguments(args, 1);

            for (var i = 0; i < _elements.Count; i++)
            {
                if (!SourceValue.EvaluateEqual(_elements[i], args[0]).ToBool())
                {
                    continue;
                }

                _elements.RemoveAt(i);
                return SourceValue.None;
            }

            throw new ArgumentException("list.remove(x): x not in list");
        }

        SourceValue Reverse(SourceValue[] args)
        {
            ValidateArguments(args);
            _elements.Reverse();
            return SourceValue.None;
        }

        Func<SourceValue[], SourceValue> GetMethod(string methodName)
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
                    throw new NotImplementedException(
                        $"Method '{methodName}' is not implemented for SourceList");
            }
        }

        public static bool ElementsEqual(SourceList a, SourceList b)
        {
            if (a._elements.Count != b._elements.Count)
            {
                return false;
            }

            for (var i = 0; i < a._elements.Count; i++)
            {
                if (!SourceValue.EvaluateEqual(a._elements[i], b._elements[i]).ToBool())
                {
                    return false;
                }
            }

            return true;
        }

        public static SourceList Concat(SourceList a, SourceList b)
        {
            var result = new SourceList();
            result._elements.AddRange(a._elements);
            result._elements.AddRange(b._elements);
            return result;
        }

        public static SourceList Repeat(SourceList a, int n)
        {
            var result = new SourceList();

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
        SourceValue GetSlice(SourceValue startValue, SourceValue stopValue, SourceValue stepValue)
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

            if (startValue.DataType == DataType.None)
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

            if (stopValue.DataType == DataType.None)
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

            var sliced = new SourceList();

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

            return new SourceValue(sliced);
        }

        static int SliceArgOrDefault(SourceValue value, int defaultValue)
        {
            if (value.DataType == DataType.None)
            {
                return defaultValue;
            }

            if (value.DataType != DataType.Long)
            {
                throw new ArgumentException($"slice indices must be integers or None, got {value.DataType}");
            }

            return (int)value.AsType<long>();
        }

        public override string ToRepresentation()
        {
            // FUTURE: once dicts/class instances exist, ToRepresentation below will grow branches for them.
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
        // a standalone string prints without quotes via ChowStr.ToSource.
        static void Repr(StringBuilder sb, SourceValue value)
        {
            switch (value.DataType)
            {
                case DataType.None:
                    sb.Append("None");
                    return;
                case DataType.Bool:
                    sb.Append(value.AsType<bool>() ? "True" : "False");
                    return;
                case DataType.Long:
                    sb.Append(value.AsType<long>());
                    return;
                case DataType.Double:
                    // Python prints `1.0`, not `1`. C#'s default float.ToSource() may drop the trailing zero.
                    var f = value.AsType<double>();
                    var fs = f.ToString("R", CultureInfo.InvariantCulture);

                    if (fs.IndexOfAny(new[] { '.', 'e', 'E', 'n', 'N', 'i', 'I' }) < 0)
                    {
                        fs += ".0";
                    }

                    sb.Append(fs);
                    return;
                case DataType.Str:
                    sb.Append('\'');
                    sb.Append(value.AsType<string>());
                    sb.Append('\'');
                    return;
                case DataType.List:
                    sb.Append(value.AsType<SourceList>());
                    return;
                case DataType.Dict:
                    sb.Append(value.AsType<SourceDictionary>());
                    return;
                case DataType.Object:
                case DataType.Range:
                default:
                    sb.Append(value.ToString());
                    return;
            }
        }

        static void ValidateArguments(SourceValue[] args, int reqArgCount = 0, DataType[] reqTypes = null)
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
                if (args[i].DataType == reqTypes[i])
                {
                    continue;
                }

                throw new ArgumentException($"Argument {i} must be of type {reqTypes[i]}, but was {args[i].DataType}");
            }
        }
    }
}
