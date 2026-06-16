using System;
using System.Collections.Generic;

namespace Chow.SourceData
{
    /// <summary>
    /// Base of the Chow object model, inspired by Python's <c>object</c>.
    /// Defaults follow CPython semantics: operations a type does not
    /// support throw <see cref="NotSupportedException"/> (Python's
    /// <c>TypeError</c>); subclasses opt in by overriding.
    /// </summary>
    abstract class SourceObject : ISourceObject
    {
        // ---------------- Construction ----------------
        
        /// <summary>(Python: <c>__init__</c>) Default accepts no state; constructor-initialized types need not override.</summary>
        public virtual void Initialize(params SourceValue[] args)
        {
        }

        // ---------------- Type ----------------

        public abstract DataType Type { get; }

        // ---------------- String representations ----------------

        /// <summary>
        /// Unambiguous, developer-facing representation.
        /// (Python: <c>__repr__</c>)
        /// </summary>
        public virtual string ToRepresentation()
        {
            return $"<{GetType().Name} object>";
        }

        public override string ToString()
        {
            return ToRepresentation();
        }

        // ---------------- Truthiness & length ----------------

        /// <summary>
        /// (Python: <c>__bool__</c>) Default is <see langword="true"/>.
        /// If the type defines a length, falls back to
        /// <c>Length() != 0</c> — mirroring Python's
        /// <c>__bool__</c> → <c>__len__</c> fallback.
        /// </summary>
        public virtual bool Truthiness
        {
            get
            {
                if (HasLength)
                {
                    return Length != 0;
                }
                return true;
            }
        }

        /// <summary>
        /// (Python: <c>__len__</c>) Override together with
        /// <see cref="HasLength"/> to make the object sized.
        /// </summary>
        public virtual int Length
        {
            get { throw new NotSupportedException(nameof(Length)); }
        }

        /// <summary>
        /// Signals whether <see cref="Length"/> is supported, so
        /// <see cref="Truthiness"/> knows whether to use it. (CPython
        /// checks for the presence of <c>__len__</c>; C# needs an
        /// explicit flag.)
        /// </summary>
        public virtual bool HasLength => false;

        // ---------------- Attribute protocol ----------------

        /// <summary>(Python: <c>__getattribute__</c>)</summary>
        public virtual SourceValue GetAttribute(SourceValue name)
        {
            throw new NotSupportedException(nameof(GetAttribute));
        }

        /// <summary>
        /// (Python: <c>__setattr__</c>) Returns nothing, like Python.
        /// </summary>
        public virtual void SetAttribute(SourceValue name, SourceValue value)
        {
            throw new NotSupportedException(nameof(SetAttribute));
        }

        /// <summary>
        /// (Python: <c>__delattr__</c>) Returns nothing — <c>del</c>
        /// yields no value.
        /// </summary>
        public virtual void DeleteAttribute(SourceValue name)
        {
            throw new NotSupportedException(nameof(DeleteAttribute));
        }

        /// <summary>(Python: <c>__dir__</c>) VariableNames only.</summary>
        public virtual List<string> Directory
        {
            get;
        }

        // ---------------- Item (container) protocol ----------------

        /// <summary>(Python: <c>__getitem__</c>)</summary>
        public virtual SourceValue GetItem(SourceValue key)
        {
            throw new NotSupportedException(nameof(GetItem));
        }

        /// <summary>(Python: <c>__setitem__</c>)</summary>
        public virtual void SetItem(SourceValue key, SourceValue value)
        {
            throw new NotSupportedException(nameof(SetItem));
        }

        /// <summary>
        /// (Python: <c>__delitem__</c>) Returns nothing — <c>del</c>
        /// yields no value.
        /// </summary>
        public virtual void DeleteItem(SourceValue key)
        {
            throw new NotSupportedException(nameof(DeleteItem));
        }

        /// <summary>(Python: <c>list.append</c> / <c>__iadd__</c> for sequences)</summary>
        public virtual void AppendItem(SourceValue value)
        {
            throw new NotSupportedException(nameof(AppendItem));
        }

        // ---------------- Membership & iteration ----------------

        /// <summary>
        /// (Python: <c>__contains__</c>) Default falls back to
        /// iterating and comparing, exactly as Python does when
        /// <c>__contains__</c> is undefined.
        /// </summary>
        public virtual bool Contains(SourceValue value)
        {
            var it = GetIterator();
            while (it.TryMoveNext(out var current))
            {
                if (Equals(current, value))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// (Python: <c>__iter__</c>) Returns a fresh iterator for the
        /// VM's for-loop dispatcher.
        /// </summary>
        public virtual IIterator GetIterator()
        {
            throw new NotSupportedException(nameof(GetIterator));
        }

        /// <summary>(Python: <c>__reversed__</c>)</summary>
        public virtual IIterator GetReversedIterator()
        {
            throw new NotSupportedException(nameof(GetReversedIterator));
        }

        // ---------------- Equality & hashing ----------------

        /// <summary>
        /// (Python: <c>__eq__</c>) Default is reference equality, same
        /// as <c>object.__eq__</c>.
        /// </summary>
        public virtual bool EqualsTo(SourceObject other)
        {
            return ReferenceEquals(this, other);
        }

        /// <summary>
        /// (Python: <c>__hash__</c>) Default is identity-based, same
        /// as <c>object.__hash__</c>.
        /// </summary>
        public virtual int HashCode()
        {
            return System.Runtime.CompilerServices
                .RuntimeHelpers.GetHashCode(this);
        }

        public sealed override bool Equals(object obj)
        {
            return obj is SourceObject so && EqualsTo(so);
        }

        public sealed override int GetHashCode() => HashCode();

        // ---------------- Callability ----------------

        /// <summary>
        /// (Python: <c>__call__</c>) Override to make instances
        /// callable.
        /// </summary>
        public virtual SourceValue Call(params SourceValue[] args)
        {
            throw new NotSupportedException(nameof(Call));
        }
        
        public virtual SourceValue Call(SourceValue arg1, SourceValue arg2, params SourceValue[] args)
        {
            throw new NotSupportedException(nameof(Call));
        }

        public SourceValue ToSourceValue()
        {
            return new SourceValue(this);
        }
    }
}