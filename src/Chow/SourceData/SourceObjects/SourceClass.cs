using System.Collections.Generic;
using Chow.Interpreter.Exceptions;

namespace Chow.SourceData
{
    /// <summary>
    /// A class object produced by a <c>class</c> declaration. Methods and class-level variables
    /// share a single attribute table, as they do in Python: a method is just a class attribute that
    /// happens to hold a <see cref="SourceFunction"/>.
    /// </summary>
    sealed class SourceClass : SourceObject
    {
        const string RepresentationFormat = "<class '{0}'>";

        // Attribute errors raised from the object itself have no instruction pointer to draw a line
        // number from; the Processor pre-checks with TryGetAttribute so its errors keep theirs.
        const int NoLineNumber = -1;

        /// <summary>The method name the VM calls when constructing an instance.</summary>
        public const string InitializerName = "__init__";

        readonly Dictionary<string, SourceValue> _attributes;

        public override DataType Type => DataType.Class;

        /// <summary>The class name as written in source. Used for diagnostics and instance repr.</summary>
        public string Name { get; }

        /// <summary>
        /// The name reported in error messages. A class names itself rather than its type, so a
        /// missing attribute reads <c>'Counter' object has no attribute 'x'</c>.
        /// </summary>
        public string TypeName => Name;

        public SourceClass(string name, Dictionary<string, SourceValue> attributes)
        {
            Name = name;
            _attributes = attributes;
        }

        /// <summary>
        /// Looks up a method or class variable. Returns <see langword="false"/> when the name is not
        /// defined, letting the caller raise the error with a source line number attached.
        /// </summary>
        public bool TryGetAttribute(string name, out SourceValue value)
        {
            return _attributes.TryGetValue(name, out value);
        }

        /// <summary>
        /// Retrieves the constructor, or returns <see langword="false"/> when the class declares
        /// none.
        /// </summary>
        public bool TryGetInitializer(out SourceFunction initializer)
        {
            if (_attributes.TryGetValue(InitializerName, out var attribute)
                && attribute.ToObject() is SourceFunction function)
            {
                initializer = function;
                return true;
            }

            initializer = null;
            return false;
        }

        public override SourceValue GetAttribute(SourceValue name)
        {
            var attrName = name.ToString();

            return TryGetAttribute(attrName, out var value)
                ? value
                : throw new AttributeException(TypeName, attrName, NoLineNumber);
        }

        public override void SetAttribute(SourceValue name, SourceValue value)
        {
            _attributes[name.ToString()] = value;
        }

        public override List<string> Directory => new List<string>(_attributes.Keys);

        public override string ToRepresentation()
        {
            return string.Format(RepresentationFormat, Name);
        }
    }
}
