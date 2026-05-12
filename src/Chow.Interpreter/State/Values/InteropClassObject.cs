using System;
using System.Collections.Generic;

namespace Chow.Interpreter.State.Values
{
    internal abstract class InteropClassObject
    {
        readonly Dictionary<string, Func<TaggedUnion[], TaggedUnion>> _methods;
        readonly Dictionary<string, Field> _fields;

        public abstract string ClassName { get; }

        protected InteropClassObject()
        {
            _methods = new Dictionary<string, Func<TaggedUnion[], TaggedUnion>>();
            _fields = new Dictionary<string, Field>();

            foreach (var entry in GetInitMethods())
            {
                _methods.Add(entry.name, entry.fn);
            }

            foreach (var entry in GetInitFields())
            {
                if (_methods.ContainsKey(entry.name))
                {
                    throw new InvalidOperationException(
                        $"'{ClassName}' declares '{entry.name}' as both a method and a field");
                }
                _fields.Add(entry.name, entry.field);
            }
        }

        // NOTE: called from base ctor BEFORE subclass ctor body runs. Subclass overrides MUST NOT
        // depend on subclass-only field initialization here. Lambdas that close over `this` are
        // safe because they execute later (after construction). Plain values are not.
        protected abstract IEnumerable<(string name, Func<TaggedUnion[], TaggedUnion> fn)> GetInitMethods();
        protected abstract IEnumerable<(string name, Field field)> GetInitFields();

        public bool HasAttribute(string name)
        {
            return _fields.ContainsKey(name) || _methods.ContainsKey(name);
        }

        public bool IsWritableField(string name)
        {
            return _fields.TryGetValue(name, out var f) && f.Set != null;
        }

        public TaggedUnion GetAttribute(string name)
        {
            if (_fields.TryGetValue(name, out var f))
            {
                return f.Get();
            }
            if (_methods.TryGetValue(name, out var m))
            {
                return new TaggedUnion(m);
            }
            throw new InvalidOperationException($"contract violation: '{ClassName}' has no attribute '{name}'");
        }

        public void SetAttribute(string name, TaggedUnion value)
        {
            if (!_fields.TryGetValue(name, out var f) || f.Set == null)
            {
                throw new InvalidOperationException(
                    $"contract violation: '{ClassName}.{name}' is not a writable field");
            }
            f.Set(value);
        }

        protected readonly struct Field
        {
            public readonly Func<TaggedUnion> Get;
            public readonly Action<TaggedUnion> Set;

            public Field(Func<TaggedUnion> get, Action<TaggedUnion> set)
            {
                Get = get;
                Set = set;
            }
        }
    }
}
