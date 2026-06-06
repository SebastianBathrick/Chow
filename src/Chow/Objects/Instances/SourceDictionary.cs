using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Chow.Exceptions;
namespace Chow.DataTypes
{
    class SourceDictionary
    {
        const string METHOD_GET_NAME = "get";
        const string METHOD_CLEAR_NAME = "clear";
        const string METHOD_POP_NAME = "pop";
        const string METHOD_UPDATE_NAME = "update";
        const string METHOD_SET_DEFAULT_NAME = "setdefault";


        readonly Dictionary<TaggedUnion, TaggedUnion> _entries = new Dictionary<TaggedUnion, TaggedUnion>();
        readonly List<TaggedUnion> _keys = new List<TaggedUnion>();

        public int Count => _keys.Count;

        public TaggedUnion this[TaggedUnion key]
        {
            get
            {
                ValidateHashable(key);

                return !_entries.TryGetValue(key, out var value) 
                    ? throw new SubscriptException(KeyRepr(key)) : value;

            }
            set => Add(key, value);
        }

        public TaggedUnion this[string name] =>
            // Will throw if method name is invalid, which is the expected behavior
            new TaggedUnion(GetMethod(name));

        public void Add(TaggedUnion key, TaggedUnion value)
        {
            ValidateHashable(key);

            if (!_entries.ContainsKey(key))
            {
                _keys.Add(key);
            }

            _entries[key] = value;
        }

        public bool ContainsKey(TaggedUnion key)
        {
            ValidateHashable(key);
            return _entries.ContainsKey(key);
        }

        TaggedUnion Get(TaggedUnion[] args)
        {
            ValidateArgRange(args, 1, 2);
            var key = args[0];
            ValidateHashable(key);

            if (_entries.TryGetValue(key, out var value))
            {
                return value;
            }

            return args.Length == 2 ? args[1] : TaggedUnion.None;
        }

        TaggedUnion Clear(TaggedUnion[] args)
        {
            ValidateArgCount(args, 0);
            _entries.Clear();
            _keys.Clear();
            return TaggedUnion.None;
        }

        TaggedUnion Pop(TaggedUnion[] args)
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

            throw new SubscriptException(KeyRepr(key));
        }

        TaggedUnion Update(TaggedUnion[] args)
        {
            ValidateArgCount(args, 1);

            if (args[0].DataType != DataType.Dict)
            {
                throw new DataTypeException($"'{args[0].DataType}' object is not a dict");
            }

            var other = args[0].AsType<SourceDictionary>();

            foreach (var key in other._keys)
            {
                Add(key, other._entries[key]);
            }

            return TaggedUnion.None;
        }

        TaggedUnion SetDefault(TaggedUnion[] args)
        {
            ValidateArgRange(args, 1, 2);
            var key = args[0];
            ValidateHashable(key);

            if (_entries.TryGetValue(key, out var existing))
            {
                return existing;
            }

            var def = args.Length == 2 ? args[1] : TaggedUnion.None;
            Add(key, def);
            return def;
        }

        public Func<TaggedUnion[], TaggedUnion> GetMethod(string methodName)
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

            throw new NotImplementedException($"Method '{methodName}' is not implemented for SourceDictionary");
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

        public static bool ElementsEqual(SourceDictionary a, SourceDictionary b)
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

                if (!a._entries[key].IsTypeAgnosticEqualTo(bValue))
                {
                    return false;
                }
            }

            return true;
        }

        public static int ElementsHashCode(SourceDictionary a)
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

        public static SourceDictionary Merge(SourceDictionary a, SourceDictionary b)
        {
            var result = new SourceDictionary();

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

        public static void ValidateHashable(TaggedUnion key)
        {
            switch (key.DataType)
            {
                case DataType.None:
                case DataType.Bool:
                case DataType.Long:
                case DataType.Double:
                case DataType.Str:
                    return;
                case DataType.Object:
                case DataType.List:
                case DataType.Dict:
                case DataType.Range:
                default:
                    throw new DataTypeException($"unhashable type: '{TypeName(key.DataType)}'");
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
        // a standalone string prints without quotes via ChowStr.ToSource.
        static void Repr(StringBuilder sb, TaggedUnion value)
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

        static string KeyRepr(TaggedUnion key)
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
                case DataType.None:
                case DataType.Bool:
                case DataType.Object:
                case DataType.Long:
                case DataType.Double:
                case DataType.Str:
                case DataType.Range:
                default:
                    return dataType.ToString().ToLowerInvariant();
            }
        }

        static void ValidateArgCount(TaggedUnion[] args, int expectedCount)
        {
            var actualCount = args?.Length ?? 0;

            if (actualCount != expectedCount)
            {
                throw new ArgumentException($"Method requires {expectedCount} arguments, but {actualCount} were provided");
            }
        }

        static void ValidateArgRange(TaggedUnion[] args, int min, int max)
        {
            var actualCount = args?.Length ?? 0;

            if (actualCount < min || actualCount > max)
            {
                throw new ArgumentException($"Method requires between {min} and {max} arguments, but {actualCount} were provided");
            }
        }
    }
}
