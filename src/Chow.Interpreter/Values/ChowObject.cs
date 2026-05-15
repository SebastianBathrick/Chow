using Chow.Interpreter.State.Values;
using System;
using System.Collections.Generic;

namespace Chow.Interpreter.Values
{
    public sealed class ChowObject : ChowValue
    {
        readonly Dictionary<string, TaggedUnion> _attributes;
        readonly Adapter _adapter;

        public string ClassName { get; }

        public object this[string name]
        {
            get { return ApiConverter.ToObject(GetTaggedAttribute(name)); }
            set { SetTaggedAttribute(name, ApiConverter.ToTaggedUnion(value)); }
        }

        public ChowObject(string className)
        {
            if (string.IsNullOrWhiteSpace(className))
            {
                throw new ArgumentException("Class name cannot be null, empty, or whitespace", nameof(className));
            }

            ClassName = className;
            _attributes = new Dictionary<string, TaggedUnion>();
            _adapter = new Adapter(this);
        }

        public bool ContainsAttribute(string name)
        {
            ValidateAttributeName(name);
            return _attributes.ContainsKey(name);
        }

        public ChowValue GetAttribute(string name)
        {
            return ApiConverter.ToChowValue(GetTaggedAttribute(name));
        }

        public override TDataType AsType<TDataType>()
        {
            if (typeof(TDataType) == typeof(bool))
            {
                return (TDataType)(object)true;
            }

            throw new InvalidCastException(GetType(), typeof(TDataType), this);
        }

        public override bool IsType<TDataType>()
        {
            return false;
        }

        public override string ToString()
        {
            return $"<{ClassName} object>";
        }

        internal InteropClassObject InteropAdapter => _adapter;

        internal static bool TryGetWrapper(object value, out ChowObject chowObject)
        {
            if (value is Adapter adapter)
            {
                chowObject = adapter.Owner;
                return true;
            }

            chowObject = null;
            return false;
        }

        internal IEnumerable<string> AttributeNames => _attributes.Keys;

        internal TaggedUnion GetTaggedAttribute(string name)
        {
            ValidateAttributeName(name);
            return _attributes[name];
        }

        internal void SetTaggedAttribute(string name, TaggedUnion value)
        {
            ValidateAttributeName(name);
            _attributes[name] = value;
            _adapter.DefineAttributeField(name);
        }

        static void ValidateAttributeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Attribute name cannot be null, empty, or whitespace", nameof(name));
            }
        }

        sealed class Adapter : InteropClassObject
        {
            readonly ChowObject _owner;

            public Adapter(ChowObject owner)
            {
                _owner = owner;
            }

            public ChowObject Owner => _owner;

            public override string ClassName => _owner.ClassName;

            protected override IEnumerable<(string name, Func<TaggedUnion[], TaggedUnion> fn)> GetInitMethods()
            {
                yield break;
            }

            protected override IEnumerable<(string name, Field field)> GetInitFields()
            {
                foreach (var name in _owner.AttributeNames)
                {
                    yield return (name, CreateField(name));
                }
            }

            protected override bool CanSetMissingAttribute(string name)
            {
                return true;
            }

            protected override void SetMissingAttribute(string name, TaggedUnion value)
            {
                _owner.SetTaggedAttribute(name, value);
            }

            public void DefineAttributeField(string name)
            {
                DefineField(name, CreateField(name));
            }

            Field CreateField(string name)
            {
                return new Field(
                    () => _owner.GetTaggedAttribute(name),
                    value => _owner.SetTaggedAttribute(name, value));
            }
        }
    }
}
