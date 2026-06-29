using System;
using Chow.SourceData;

namespace Chow
{
    /// <summary>
    /// Represents a single Chow object, such as an <c>int</c>, <c>float</c>, <c>str</c>, <c>bool</c>,
    /// <c>None</c>, <c>list</c>, <c>dict</c>, or <c>scope</c>.
    /// <para>
    /// A Chow object can be created from and converted to common .NET types, and provides access to
    /// the items, attributes, and methods of Chow collections and callables.
    /// </para>
    /// </summary>
    public sealed class ChowObject : IChowObject
    {
        ISourceObject _srcObj;

        /// <summary>The Chow <c>None</c> object, representing the absence of a value.</summary>
        public static ChowObject None { get; }
            = (ChowObject)ApiConverter.Convert(SourceValue.None);

        internal SourceValue SourceValue { get; }

        /// <inheritdoc/>
        public bool IsNone => SourceValue.IsNone;

        /// <inheritdoc/>
        public bool IsList => SourceValue.IsList;

        /// <inheritdoc/>
        public bool IsDictionary => SourceValue.IsDictionary;

        /// <inheritdoc/>
        public bool IsScope => SourceValue.IsScope;

        ISourceObject SourceObject => _srcObj ?? (_srcObj = SourceValue.ToISourceObject());

        /// <summary>
        /// The number of items in this object, for collection-like objects such as lists,
        /// dictionaries, strings, and ranges.
        /// </summary>
        public int Length => SourceObject.Length;

        /// <summary>
        /// Gets or sets the item associated with <paramref name="key"/>, for indexable objects such
        /// as lists and dictionaries. For lists, the key is the integer index; for dictionaries, it
        /// is the entry's key.
        /// </summary>
        /// <param name="key">The index or key identifying the item to read or write.</param>
        /// <returns>The object stored at <paramref name="key"/>.</returns>
        public ChowObject this[ChowObject key]
        {
            get => new ChowObject(SourceObject.GetItem(key.SourceValue));
            set => SourceObject.SetItem(key.SourceValue, value.SourceValue);
        }
        
        // This is primarily for testing. Avoid using internally if possible
        internal ChowObject(SourceValue srcVal)
        {
            SourceValue = srcVal;
        }

        internal ChowObject(ref SourceValue srcVal)
        {
            SourceValue = srcVal;
        }
        
        /// <summary>Retrieves a named attribute from this object.</summary>
        /// <param name="name">The name of the attribute to retrieve.</param>
        /// <returns>The requested attribute.</returns>
        public ChowObject GetAttribute(ChowObject name)
        {
            var attr = SourceObject.GetAttribute(name.SourceValue);
            var chowVal = new ChowObject(attr);
            return chowVal;
        }

        /// <summary>
        /// Invokes a named method on this object, passing the supplied arguments.
        /// </summary>
        /// <param name="methodName">The name of the method to invoke.</param>
        /// <param name="args">The arguments to pass to the method.</param>
        /// <returns>The object returned by the method.</returns>
        public ChowObject Call(string methodName, params ChowObject[] args)
        {
            var methodAttr = SourceObject.GetAttribute(methodName);
            var convertedArgs = ApiConverter.ConvertToInterface(args);
            
            return (ChowObject)ChowEngine.Call(ref methodAttr, convertedArgs);
        }

        /// <summary>Returns this object as the requested host type.</summary>
        /// <typeparam name="T">The host type to return the object as.</typeparam>
        /// <returns>This object as <typeparamref name="T"/>.</returns>
        public T As<T>()
        {
            return (T)SourceValue.ToObject();
        }


        /// <summary>
        /// Returns the Chow string representation of this object, matching how Chow's <c>str</c>
        /// renders it.
        /// </summary>
        /// <returns>The string representation of this object.</returns>
        public override string ToString()
        {
            return SourceValue;
        }

        #region Create Methods

        public static ChowObject Create(object netVal)
        {
            return (ChowObject)ChowObjectFactory.CreateObject(netVal);
        }

        /// <summary>Creates a new, empty Chow list.</summary>
        /// <returns>An empty Chow list.</returns>
        public static ChowObject CreateList()
        {
            // Cast, because IChowObject has no public API or implicit operators for the client
            return (ChowObject)ChowObjectFactory.CreateList();
        }

        /// <summary>Creates a new, empty Chow dictionary.</summary>
        /// <returns>An empty Chow dictionary.</returns>
        public static ChowObject CreateDictionary()
        {
            // Cast, because IChowObject has no public API or implicit operators for the client
            return (ChowObject)ChowObjectFactory.CreateDict();
        }

        /// <summary>
        /// Creates a new, empty Chow scope, which can be passed to the engine to share state across
        /// executions.
        /// </summary>
        /// <returns>An empty Chow scope.</returns>
        public static ChowObject CreateScope()
        {
            // Cast, because IChowObject has no public API or implicit operators for the client
            return (ChowObject)ChowObjectFactory.CreateScope();
        }

        #endregion

        #region Implicit Operators

        /// <summary>Converts a host <see cref="bool"/> to a Chow <c>bool</c>.</summary>
        public static implicit operator ChowObject(bool value)
        {
            return new ChowObject(new SourceValue(value));
        }

        /// <summary>Converts this object to a host <see cref="bool"/>.</summary>
        public static implicit operator bool(ChowObject @object)
        {
            return @object.SourceValue.ToBool();
        }

        /// <summary>Converts a host <see cref="long"/> to a Chow <c>int</c>.</summary>
        public static implicit operator ChowObject(long value)
        {
            return new ChowObject(value);
        }

        /// <summary>Converts this object to a host <see cref="long"/>.</summary>
        public static implicit operator long(ChowObject @object)
        {
            return @object.SourceValue.ToLong();
        }

        /// <summary>Converts a host <see cref="double"/> to a Chow <c>float</c>.</summary>
        public static implicit operator ChowObject(double value)
        {
            return new ChowObject(value);
        }

        /// <summary>Converts this object to a host <see cref="double"/>.</summary>
        public static implicit operator double(ChowObject @object)
        {
            return @object.SourceValue.ToDouble();
        }

        /// <summary>Converts a host <see cref="string"/> to a Chow <c>str</c>.</summary>
        public static implicit operator ChowObject(string value)
        {
            return new ChowObject(value);
        }

        /// <summary>
        /// Converts a host delegate into a Chow callable that takes no arguments and returns an
        /// object.
        /// </summary>
        public static implicit operator ChowObject(Func<object> value)
        {
            SourceValue Wrapper(SourceValue[] _)
            {
                var result = value();
                return result is null ? SourceValue.None : new SourceValue(result);
            }

            return new ChowObject(new SourceValue((Func<SourceValue[], SourceValue>)Wrapper));
        }

        /// <summary>
        /// Converts a host delegate into a Chow callable that takes a single argument and returns
        /// nothing.
        /// </summary>
        public static implicit operator ChowObject(Action<object> value)
        {
            SourceValue Wrapper(SourceValue[] args)
            {
                value(args != null && args.Length > 0 ? args[0].ToObject() : null);
                return SourceValue.None;
            }

            return new ChowObject(new SourceValue((Func<SourceValue[], SourceValue>)Wrapper));
        }

        /// <summary>
        /// Converts a host delegate into a Chow callable that takes multiple arguments and returns
        /// nothing.
        /// </summary>
        public static implicit operator ChowObject(Action<object[]> value)
        {
            SourceValue Wrapper(SourceValue[] args)
            {
                value(SourceValue.ToObjects(args ?? Array.Empty<SourceValue>()));
                return SourceValue.None;
            }

            return new ChowObject(new SourceValue((Func<SourceValue[], SourceValue>)Wrapper));
        }

        /// <summary>
        /// Converts a host delegate into a Chow callable that takes multiple arguments and returns
        /// an object.
        /// </summary>
        public static implicit operator ChowObject(Func<object[], object> value)
        {
            SourceValue Wrapper(SourceValue[] args)
            {
                var result = value(SourceValue.ToObjects(args ?? Array.Empty<SourceValue>()));
                return result is null ? SourceValue.None : new SourceValue(result);
            }

            return new ChowObject(new SourceValue((Func<SourceValue[], SourceValue>)Wrapper));
        }

        /// <summary>
        /// Determines whether two <see cref="ChowObject"/>s represent equal objects.
        /// </summary>
        public static bool operator ==(ChowObject l, ChowObject r)
        {
            if (ReferenceEquals(l, r))
            {
                return true;
            }

            if (l is null || r is null)
            {
                return false;
            }

            return l.SourceValue.Equals(r.SourceValue);
        }

        /// <summary>
        /// Determines whether two <see cref="ChowObject"/>s represent unequal objects.
        /// </summary>
        public static bool operator !=(ChowObject l, ChowObject r)
        {
            return !(l == r);
        }

        /// <summary>Determines whether this object equals a host <see cref="bool"/>.</summary>
        public static bool operator ==(ChowObject l, bool r)
        {
            return l?.SourceValue.ToBool() == r;
        }

        /// <summary>
        /// Determines whether this object differs from a host <see cref="bool"/>.
        /// </summary>
        public static bool operator !=(ChowObject l, bool r)
        {
            return !(l == r);
        }

        /// <summary>Determines whether this object equals a host <see cref="long"/>.</summary>
        public static bool operator ==(ChowObject l, long r)
        {
            return l?.SourceValue.ToLong() == r;
        }

        /// <summary>
        /// Determines whether this object differs from a host <see cref="long"/>.
        /// </summary>
        public static bool operator !=(ChowObject l, long r)
        {
            return !(l == r);
        }

        /// <summary>
        /// Determines whether this object equals a host <see cref="double"/>.
        /// </summary>
        public static bool operator ==(ChowObject l, double r)
        {
            return !(l is null) && l.SourceValue.ToDouble().Equals(r);
        }

        /// <summary>
        /// Determines whether this object differs from a host <see cref="double"/>.
        /// </summary>
        public static bool operator !=(ChowObject l, double r)
        {
            return !(l == r);
        }

        #endregion

        #region Equality Methods

        /// <summary>
        /// Determines whether this object equals the object represented by another
        /// <see cref="ChowObject"/>.
        /// </summary>
        /// <param name="obj">The object to compare against.</param>
        /// <returns><c>true</c> if <paramref name="obj"/> is a <see cref="ChowObject"/>
        /// representing an equal object; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return obj is ChowObject other && SourceValue.Equals(other.SourceValue);
        }

        /// <summary>Returns a hash code for this object.</summary>
        /// <returns>A hash code for this object.</returns>
        public override int GetHashCode()
        {
            return SourceValue.GetHashCode();
        }

        #endregion


    }
}
