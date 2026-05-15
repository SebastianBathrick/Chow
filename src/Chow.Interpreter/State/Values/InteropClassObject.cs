using System;
using System.Collections.Generic;

namespace Chow.Interpreter.State.Values
{
    internal abstract class InteropClassObject
    {
        readonly Dictionary<string, Func<TaggedUnion[], TaggedUnion>> _methods;
        readonly Dictionary<string, Field> _fields;
        bool _initialized;

        public abstract string ClassName { get; }

        protected InteropClassObject()
        {
            _methods = new Dictionary<string, Func<TaggedUnion[], TaggedUnion>>();
            _fields = new Dictionary<string, Field>();
        }

        protected abstract IEnumerable<(string name, Func<TaggedUnion[], TaggedUnion> fn)> GetInitMethods();
        protected abstract IEnumerable<(string name, Field field)> GetInitFields();

        protected virtual bool CanSetMissingAttribute(string name)
        {
            return false;
        }

        protected virtual void SetMissingAttribute(string name, TaggedUnion value)
        {
            throw new InvalidOperationException(
                $"contract violation: '{ClassName}.{name}' is not a writable field");
        }

        protected void DefineField(string name, Field field)
        {
            EnsureInitialized();

            if (_methods.ContainsKey(name))
            {
                throw new InvalidOperationException(
                    $"'{ClassName}' declares '{name}' as both a method and a field");
            }

            _fields[name] = field;
        }

        public bool HasAttribute(string name)
        {
            EnsureInitialized();
            return _fields.ContainsKey(name) || _methods.ContainsKey(name);
        }

        public bool IsWritableField(string name)
        {
            EnsureInitialized();
            return _fields.TryGetValue(name, out var f) && f.Set != null;
        }

        public bool CanSetAttribute(string name)
        {
            EnsureInitialized();

            if (_fields.TryGetValue(name, out var f))
            {
                return f.Set != null;
            }

            return !_methods.ContainsKey(name) && CanSetMissingAttribute(name);
        }

        public TaggedUnion GetAttribute(string name)
        {
            EnsureInitialized();

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
            EnsureInitialized();

            if (_fields.TryGetValue(name, out var f))
            {
                if (f.Set != null)
                {
                    f.Set(value);
                    return;
                }

                throw new InvalidOperationException(
                    $"contract violation: '{ClassName}.{name}' is not a writable field");
            }

            if (!_methods.ContainsKey(name) && CanSetMissingAttribute(name))
            {
                SetMissingAttribute(name, value);
                return;
            }

            throw new InvalidOperationException(
                $"contract violation: '{ClassName}.{name}' is not a writable field");
        }

        void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

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

            _initialized = true;
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
