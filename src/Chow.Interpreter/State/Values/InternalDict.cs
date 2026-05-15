using Chow.Interpreter.Exceptions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Chow.Interpreter.State.Values
{
    class InternalDict
    {
        const string METHOD_GET_NAME = "get";
        const string METHOD_CLEAR_NAME = "clear";
        const string METHOD_POP_NAME = "pop";
        const string METHOD_UPDATE_NAME = "update";
        const string METHOD_SET_DEFAULT_NAME = "setdefault";


        readonly Dictionary<TaggedUnion, TaggedUnion> _entries;
        readonly List<TaggedUnion> _keys;

        public int Count => _keys.Count;

        public TaggedUnion this[TaggedUnion key]
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

        public TaggedUnion this[string name] =>
            // Will throw if method name is invalid, which is the expected behavior
            new TaggedUnion(GetMethod(name));

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
            throw new DictKeyException(KeyRepr(key));
        }

        TaggedUnion Update(TaggedUnion[] args)
        {
            ValidateArgCount(args, 1);
            if (args[0].Tag != Tag.Dict)
            {
                throw new TypeException($"'{args[0].Tag}' object is not a dict");
            }
            var other = args[0].DictValue;
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
                if (a._entries[key] != bValue)
                {
                    return false;
                }
            }
            return true;
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
                    throw new TypeException($"unhashable type: '{TypeName(key.Tag)}'");
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
                    var f = value.FloatValue;
                    var fs = f.ToString("R", CultureInfo.InvariantCulture);
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
                    sb.Append(value.ListValue);
                    return;
                
                case Tag.Dict:
                    sb.Append(value.DictValue);
                    return;
                
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
