using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Chow.Interpreter.Exceptions;
namespace Chow.Interpreter.State.Values
{
    class InternalDict
    {
        const string METHOD_GET_NAME = "get";
        const string METHOD_CLEAR_NAME = "clear";
        const string METHOD_POP_NAME = "pop";
        const string METHOD_UPDATE_NAME = "update";
        const string METHOD_SET_DEFAULT_NAME = "setdefault";


        readonly Dictionary<ChowValue, ChowValue> _entries;
        readonly List<ChowValue> _keys;

        public int Count => _keys.Count;

        public ChowValue this[ChowValue key]
        {
            get
            {
                ValidateHashable(key);

                if (!_entries.TryGetValue(key, out var value))
                {
                    throw new DictKeyException(KeyRepr(key));
                }

                return value;
            }
            set => Add(key, value);
        }

        public ChowValue this[string name] =>
            // Will throw if method name is invalid, which is the expected behavior
            new ChowValue(GetMethod(name));

        public InternalDict()
        {
            _entries = new Dictionary<ChowValue, ChowValue>();
            _keys = new List<ChowValue>();
        }

        public void Add(ChowValue key, ChowValue value)
        {
            ValidateHashable(key);

            if (!_entries.ContainsKey(key))
            {
                _keys.Add(key);
            }

            _entries[key] = value;
        }

        public bool ContainsKey(ChowValue key)
        {
            ValidateHashable(key);
            return _entries.ContainsKey(key);
        }

        ChowValue Get(ChowValue[] args)
        {
            ValidateArgRange(args, 1, 2);
            var key = args[0];
            ValidateHashable(key);

            if (_entries.TryGetValue(key, out var value))
            {
                return value;
            }

            return args.Length == 2 ? args[1] : ChowValue.None;
        }

        ChowValue Clear(ChowValue[] args)
        {
            ValidateArgCount(args, 0);
            _entries.Clear();
            _keys.Clear();
            return ChowValue.None;
        }

        ChowValue Pop(ChowValue[] args)
        {
            ValidateArgRange(args, 1, 2);
            var key = args[0];
            ValidateHashable(key);

            if (_entries.TryGetValue(key, out var value))
            {
                _entries.Remove(key);
                _keys.Remove(key);
                return value;
            }

            if (args.Length == 2)
            {
                return args[1];
            }

            throw new DictKeyException(KeyRepr(key));
        }

        ChowValue Update(ChowValue[] args)
        {
            ValidateArgCount(args, 1);

            if (args[0].DataType != DataType.Dict)
            {
                throw new TypeException($"'{args[0].DataType}' object is not a dict");
            }

            var other = args[0].AsType<InternalDict>();

            foreach (var key in other._keys)
            {
                Add(key, other._entries[key]);
            }

            return ChowValue.None;
        }

        ChowValue SetDefault(ChowValue[] args)
        {
            ValidateArgRange(args, 1, 2);
            var key = args[0];
            ValidateHashable(key);

            if (_entries.TryGetValue(key, out var existing))
            {
                return existing;
            }

            var def = args.Length == 2 ? args[1] : ChowValue.None;
            Add(key, def);
            return def;
        }

        public Func<ChowValue[], ChowValue> GetMethod(string methodName)
        {
            switch (methodName)
            {
                case METHOD_GET_NAME:
                    return Get;
                case METHOD_CLEAR_NAME:
                    return Clear;
                case METHOD_POP_NAME:
                    return Pop;
                case METHOD_UPDATE_NAME:
                    return Update;
                case METHOD_SET_DEFAULT_NAME:
                    return SetDefault;
            }

            throw new NotImplementedException($"Method '{methodName}' is not implemented for InternalDict");
        }

        public bool HasMethod(string methodName)
        {
            switch (methodName)
            {
                case METHOD_GET_NAME:
                case METHOD_CLEAR_NAME:
                case METHOD_POP_NAME:
                case METHOD_UPDATE_NAME:
                case METHOD_SET_DEFAULT_NAME:
                    return true;
                default:
                    return false;
            }
        }

        public static bool ElementsEqual(InternalDict a, InternalDict b)
        {
            if (a._entries.Count != b._entries.Count)
            {
                return false;
            }

            foreach (var key in a._keys)
            {
                if (!b._entries.TryGetValue(key, out var bValue))
                {
                    return false;
                }

                if (!a._entries[key].IsEqualTo(bValue))
                {
                    return false;
                }
            }

            return true;
        }

        public static int ElementsHashCode(InternalDict a)
        {
            // Order-independent combine — paired with ElementsEqual's order-insensitive lookup.
            unchecked
            {
                var hash = 0;

                foreach (var key in a._keys)
                {
                    var keyHash = key.GetHashCode();
                    var valueHash = a._entries[key].GetHashCode();
                    hash ^= keyHash * 31 ^ valueHash;
                }

                return hash;
            }
        }

        public static InternalDict Merge(InternalDict a, InternalDict b)
        {
            var result = new InternalDict();

            foreach (var key in a._keys)
            {
                result.Add(key, a._entries[key]);
            }

            foreach (var key in b._keys)
            {
                result.Add(key, b._entries[key]);
            }

            return result;
        }

        public static void ValidateHashable(ChowValue key)
        {
            switch (key.DataType)
            {
                case DataType.None:
                case DataType.Bool:
                case DataType.Int:
                case DataType.Float:
                case DataType.Str:
                    return;
                default:
                    throw new TypeException($"unhashable type: '{TypeName(key.DataType)}'");
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append('{');

            for (var i = 0; i < _keys.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                var key = _keys[i];
                Repr(sb, key);
                sb.Append(": ");
                Repr(sb, _entries[key]);
            }

            sb.Append('}');
            return sb.ToString();
        }

        // Python-faithful repr used inside collection contexts: strings get single quotes here, even though
        // a standalone string prints without quotes via ChowStr.ToString.
        static void Repr(StringBuilder sb, ChowValue value)
        {
            switch (value.DataType)
            {
                case DataType.None:
                    sb.Append("None");
                    return;

                case DataType.Bool:
                    sb.Append(value.AsType<bool>() ? "True" : "False");
                    return;

                case DataType.Int:
                    sb.Append(value.AsType<long>());
                    return;

                case DataType.Float:
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
                    sb.Append(value.AsType<InternalList>());
                    return;

                case DataType.Dict:
                    sb.Append(value.AsType<InternalDict>());
                    return;

                default:
                    sb.Append(value.ToString());
                    return;
            }
        }

        static string KeyRepr(ChowValue key)
        {
            var sb = new StringBuilder();
            Repr(sb, key);
            return sb.ToString();
        }

        static string TypeName(DataType dataType)
        {
            switch (dataType)
            {
                case DataType.List:
                    return "list";
                case DataType.Dict:
                    return "dict";
                default:
                    return dataType.ToString().ToLowerInvariant();
            }
        }

        static void ValidateArgCount(ChowValue[] args, int expectedCount)
        {
            var actualCount = args?.Length ?? 0;

            if (actualCount != expectedCount)
            {
                throw new ArgumentException($"Method requires {expectedCount} arguments, but {actualCount} were provided");
            }
        }

        static void ValidateArgRange(ChowValue[] args, int min, int max)
        {
            var actualCount = args?.Length ?? 0;

            if (actualCount < min || actualCount > max)
            {
                throw new ArgumentException($"Method requires between {min} and {max} arguments, but {actualCount} were provided");
            }
        }
    }
}
