using System;
using System.Collections.Generic;
using System.Text;
using Chow.Interpreter.Exceptions;

namespace Chow.SourceData
{
    class SourceDict : SourceObject
    {
        const string GetMethodName = "get";
        const string ClearMethodName = "clear";
        const string PopMethodName = "pop";
        const string UpdateMethodName = "update";
        const string SetMethodName = "setdefault";


        static readonly List<string> MethodNames = new List<string>
        {
            GetMethodName,
            ClearMethodName,
            PopMethodName,
            UpdateMethodName,
            SetMethodName
        };

        readonly Dictionary<SourceValue, SourceValue> _elements = new Dictionary<SourceValue, SourceValue>();
        readonly List<SourceValue> _keys = new List<SourceValue>();

        // Lazily created on first attribute access; most dicts never look up a method.
        Dictionary<string, SourceValue> _methodCache;

        public override DataType Type => DataType.Dict;

        public override bool HasLength => true;

        public override int Length => _keys.Count;

        public override SourceValue GetItem(SourceValue key)
        {
            return _elements[key];
        }

        public override void SetItem(SourceValue key, SourceValue value)
        {
            Add(key, value);
        }

        public override void DeleteItem(SourceValue key)
        {
            ValidateHashable(key);

            if (!_elements.Remove(key))
            {
                throw new SubscriptException(key);
            }

            _keys.Remove(key);
        }

        public override bool Contains(SourceValue value)
        {
            return ContainsKey(value);
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
            ValidateHashable(key);

            var countBefore = _elements.Count;
            _elements[key] = value;

            // Count grew => the key was new; record insertion order.
            if (_elements.Count != countBefore)
            {
                _keys.Add(key);
            }
        }

        public bool ContainsKey(SourceValue key)
        {
            ValidateHashable(key);
            return _elements.ContainsKey(key);
        }

        SourceValue Get(SourceValue[] args)
        {
            ValidateArgRange(args, 1, 2);
            var key = args[0];
            ValidateHashable(key);

            if (_elements.TryGetValue(key, out var value))
            {
                return value;
            }

            return args.Length == 2 ? args[1] : SourceValue.None;
        }

        SourceValue Clear(SourceValue[] args)
        {
            ValidateArgumentCount(args, 0);
            _elements.Clear();
            _keys.Clear();
            return SourceValue.None;
        }

        SourceValue Pop(SourceValue[] args)
        {
            ValidateArgRange(args, 1, 2);
            var key = args[0];
            ValidateHashable(key);

            if (_elements.TryGetValue(key, out var value))
            {
                _elements.Remove(key);
                _keys.Remove(key);
                return value;
            }

            return args.Length == 2 ? args[1] : throw new SubscriptException(key);

        }

        SourceValue Update(SourceValue[] args)
        {
            ValidateArgumentCount(args, 1);

            if (args[0].DataType != DataType.Dict)
            {
                throw new DataTypeException($"'{args[0].DataType}' object is not a dict");
            }

            var other = (SourceDict)args[0].ToObject();

            foreach (var key in other._elements.Keys)
            {
                Add(key, other._elements[key]);
            }

            return SourceValue.None;
        }

        SourceValue SetDefault(SourceValue[] args)
        {
            ValidateArgRange(args, 1, 2);
            var key = args[0];
            ValidateHashable(key);

            if (_elements.TryGetValue(key, out var existing))
            {
                return existing;
            }

            var def = args.Length == 2 ? args[1] : SourceValue.None;
            Add(key, def);
            return def;
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
            }

            throw new NotImplementedException($"Method '{methodName}' is not implemented for SourceDict");
        }

        public static bool ElementsEqual(SourceDict a, SourceDict b)
        {
            if (a._elements.Count != b._elements.Count)
            {
                return false;
            }

            foreach (var key in a._keys)
            {
                if (!b._elements.TryGetValue(key, out var bValue))
                {
                    return false;
                }

                if (!SourceValue.IsEqual(a._elements[key], bValue).ToBool())
                {
                    return false;
                }
            }

            return true;
        }

        public static SourceDict Merge(SourceDict a, SourceDict b)
        {
            var result = new SourceDict();

            foreach (var key in a._keys)
            {
                result.Add(key, a._elements[key]);
            }

            foreach (var key in b._keys)
            {
                result.Add(key, b._elements[key]);
            }

            return result;
        }

        static void ValidateHashable(SourceValue key)
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

        public override string ToRepresentation()
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
                sb.Append($"{key}: {_elements[key]}");
            }

            sb.Append('}');
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

        static void ValidateArgumentCount(SourceValue[] args, int expectedCount)
        {
            var actualCount = args?.Length ?? 0;

            if (actualCount == expectedCount)
            {
                return;
            }

            throw new ArgumentException(
                $"Method requires {expectedCount} arguments, but {actualCount} were provided");
        }

        static void ValidateArgRange(SourceValue[] args, int min, int max)
        {
            var actualCount = args?.Length ?? 0;

            if (actualCount >= min && actualCount <= max)
            {
                return;
            }

            throw new ArgumentException(
                $"Method requires between {min} and {max} arguments, but "
                + $"{actualCount} were provided");

        }
    }
}
