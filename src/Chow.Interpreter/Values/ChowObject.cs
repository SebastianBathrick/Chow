using Chow.Interpreter.State.Values;
using System;
using System.Collections.Generic;

namespace Chow.Interpreter.Values
{
    /// <summary>
    /// A host-defined object that can be injected into the Chow scope and accessed from Chow source by
    /// attribute access (<c>obj.attr</c>). Attributes are get/set via the indexer and are created on first
    /// write.
    /// </summary>
    public sealed class ChowObject : ChowValue
    {
        readonly Dictionary<string, TaggedUnion> _attributes;
        readonly Adapter _adapter;

        /// <summary>Gets the class name provided at construction. Used for display purposes only.</summary>
        public string ClassName { get; }

        /// <summary>
        /// Gets or sets an attribute by name. The attribute is created on first write. The getter returns the
        /// attribute value as a boxed primitive or a <see cref="ChowValue"/> subclass. The setter accepts the
        /// same value types as the <see cref="ChowModule"/> indexer setter.
        /// </summary>
        /// <param name="name">The attribute name. Must not be <see langword="null"/>, empty, or whitespace.</param>
        /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/>, empty, or whitespace.</exception>
        public object this[string name]
        {
            get { return ApiConverter.ToObject(GetTaggedAttribute(name)); }
            set { SetTaggedAttribute(name, ApiConverter.ToTaggedUnion(value)); }
        }

        /// <summary>
        /// Initialises a new <see cref="ChowObject"/> with the given class name.
        /// </summary>
        /// <param name="className">
        /// The display name for this object's type. Must not be <see langword="null"/>, empty, or whitespace.
        /// </param>
        /// <exception cref="ArgumentException">
        /// <paramref name="className"/> is <see langword="null"/>, empty, or whitespace.
        /// </exception>
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

        /// <summary>Returns <see langword="true"/> if the named attribute exists on this object.</summary>
        /// <param name="name">The attribute name. Must not be <see langword="null"/>, empty, or whitespace.</param>
        /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/>, empty, or whitespace.</exception>
        public bool ContainsAttribute(string name)
        {
            ValidateAttributeName(name);
            return _attributes.ContainsKey(name);
        }

        /// <summary>Returns the value of an attribute as a <see cref="ChowValue"/>.</summary>
        /// <param name="name">The attribute name. Must not be <see langword="null"/>, empty, or whitespace.</param>
        /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/>, empty, or whitespace.</exception>
        public ChowValue GetAttribute(string name)
        {
            return ApiConverter.ToChowValue(GetTaggedAttribute(name));
        }

        /// <summary>
        /// Extracts the underlying value as <typeparamref name="TDataType"/>. Supported conversions:
        /// <see langword="bool"/> (always <see langword="true"/>).
        /// </summary>
        /// <exception cref="InvalidCastException"><typeparamref name="TDataType"/> is not a supported conversion.</exception>
        public override TDataType AsType<TDataType>()
        {
            if (typeof(TDataType) == typeof(bool))
            {
                return (TDataType)(object)true;
            }

            throw new InvalidCastException(GetType(), typeof(TDataType), this);
        }

        /// <summary>Always returns <see langword="false"/>.</summary>
        public override bool IsType<TDataType>()
        {
            return false;
        }

        /// <summary>Returns <c>"&lt;ClassName object&gt;"</c>.</summary>
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
