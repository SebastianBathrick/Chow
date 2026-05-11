using Chow.Interpreter.Exceptions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Chow.Interpreter.Values.Internal
{
    internal class InternalDict
    {
        const string METHOD_GET_NAME = "get";
        const string METHOD_CLEAR_NAME = "clear";
        const string METHOD_POP_NAME = "pop";
        const string METHOD_UPDATE_NAME = "update";
        const string METHOD_SETDEFAULT_NAME = "setdefault";


        Dictionary<TaggedUnion, TaggedUnion> _entries;
        List<TaggedUnion> _keys;

        public int Count => _keys.Count;

        public TaggedUnion this[TaggedUnion key]
        {
            get
            {
                ValidateHashable(key);
                if (!_entries.TryGetValue(key, out TaggedUnion value))
                {
                    throw new ChowKeyErrorException(KeyRepr(key));
                }
                return value;
            }
            set
            {
                Add(key, value);
            }
        }

        public TaggedUnion this[string name]
        {
            get
            {
                // Will throw if method name is invalid, which is the expected behavior
                return new TaggedUnion(GetMethod(name));
            }
        }

        public InternalDict()
        {
            _entries = new Dictionary<TaggedUnion, TaggedUnion>();
            _keys = new List<TaggedUnion>();
        }

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
            TaggedUnion key = args[0];
            ValidateHashable(key);
            if (_entries.TryGetValue(key, out TaggedUnion value))
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
            TaggedUnion key = args[0];
            ValidateHashable(key);
            if (_entries.TryGetValue(key, out TaggedUnion value))
            {
                _entries.Remove(key);
                _keys.Remove(key);
                return value;
            }
            if (args.Length == 2)
            {
                return args[1];
            }
            throw new ChowKeyErrorException(KeyRepr(key));
        }

        TaggedUnion Update(TaggedUnion[] args)
        {
            ValidateArgCount(args, 1);
            if (args[0].Tag != Tag.Dict)
            {
                throw new ChowTypeErrorException($"'{args[0].Tag}' object is not a dict");
            }
            InternalDict other = args[0].DictValue;
            foreach (TaggedUnion key in other._keys)
            {
                Add(key, other._entries[key]);
            }
            return TaggedUnion.None;
        }

        TaggedUnion SetDefault(TaggedUnion[] args)
        {
            ValidateArgRange(args, 1, 2);
            TaggedUnion key = args[0];
            ValidateHashable(key);
            if (_entries.TryGetValue(key, out TaggedUnion existing))
            {
                return existing;
            }
            TaggedUnion def = args.Length == 2 ? args[1] : TaggedUnion.None;
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
                case METHOD_SETDEFAULT_NAME:
                    return SetDefault;
                default:
                    throw new NotImplementedException($"Method '{methodName}' is not implemented for InternalDict");
            }
        }

        public bool HasMethod(string methodName)
        {
            switch (methodName)
            {
                case METHOD_GET_NAME:
                case METHOD_CLEAR_NAME:
                case METHOD_POP_NAME:
                case METHOD_UPDATE_NAME:
                case METHOD_SETDEFAULT_NAME:
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
            foreach (TaggedUnion key in a._keys)
            {
                if (!b._entries.TryGetValue(key, out TaggedUnion bValue))
                {
                    return false;
                }
                if (a._entries[key] != bValue)
                {
                    return false;
                }
            }
            return true;
        }

        public static InternalDict Merge(InternalDict a, InternalDict b)
        {
            InternalDict result = new InternalDict();
            foreach (TaggedUnion key in a._keys)
            {
                result.Add(key, a._entries[key]);
            }
            foreach (TaggedUnion key in b._keys)
            {
                result.Add(key, b._entries[key]);
            }
            return result;
        }

        public static void ValidateHashable(TaggedUnion key)
        {
            switch (key.Tag)
            {
                case Tag.None:
                case Tag.Boolean:
                case Tag.Int:
                case Tag.Float:
                case Tag.Str:
                    return;
                default:
                    throw new ChowTypeErrorException($"unhashable type: '{TypeName(key.Tag)}'");
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('{');
            for (int i = 0; i < _keys.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }
                TaggedUnion key = _keys[i];
                Repr(sb, key);
                sb.Append(": ");
                Repr(sb, _entries[key]);
            }
            sb.Append('}');
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
                    float f = value.FloatValue;
                    string fs = f.ToString("R", CultureInfo.InvariantCulture);
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

        static string KeyRepr(TaggedUnion key)
        {
            StringBuilder sb = new StringBuilder();
            Repr(sb, key);
            return sb.ToString();
        }

        static string TypeName(Tag tag)
        {
            switch (tag)
            {
                case Tag.List:
                    return "list";
                case Tag.Dict:
                    return "dict";
                default:
                    return tag.ToString().ToLowerInvariant();
            }
        }

        static void ValidateArgCount(TaggedUnion[] args, int expectedCount)
        {
            int actualCount = args?.Length ?? 0;
            if (actualCount != expectedCount)
            {
                throw new ArgumentException($"Method requires {expectedCount} arguments, but {actualCount} were provided");
            }
        }

        static void ValidateArgRange(TaggedUnion[] args, int min, int max)
        {
            int actualCount = args?.Length ?? 0;
            if (actualCount < min || actualCount > max)
            {
                throw new ArgumentException($"Method requires between {min} and {max} arguments, but {actualCount} were provided");
            }
        }
    }
}
