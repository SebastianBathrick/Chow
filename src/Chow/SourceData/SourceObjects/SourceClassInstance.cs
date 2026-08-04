using System.Collections.Generic;
using Chow.Interpreter.Exceptions;

namespace Chow.SourceData
{
    /// <summary>
    /// An instance produced by calling a <see cref="SourceClass"/>. Fields assigned through
    /// <c>self</c> live in this object's own table; anything not found there falls through to the
    /// class, which is how methods and class variables are reached.
    /// </summary>
    sealed class SourceClassInstance : SourceObject
    {
        const string RepresentationFormat = "<{0} object>";

        // Attribute errors raised from the object itself have no instruction pointer to draw a line
        // number from; the Processor pre-checks with TryGetAttribute so its errors keep theirs.
        const int NoLineNumber = -1;

        readonly Dictionary<string, SourceValue> _fields = new Dictionary<string, SourceValue>();

        public override DataType Type => DataType.Instance;

        /// <summary>The class this instance was constructed from.</summary>
        public SourceClass Class { get; }

        /// <summary>
        /// The name reported in error messages. Instances report their class, so a missing attribute
        /// reads <c>'Counter' object has no attribute 'x'</c>.
        /// </summary>
        public string TypeName => Class.Name;

        public SourceClassInstance(SourceClass sourceClass)
        {
            Class = sourceClass;
        }

        /// <summary>
        /// Resolves an attribute against this instance's fields, then its class. A class attribute
        /// holding a function is bound to this instance so the call site does not have to pass
        /// <c>self</c> explicitly. Returns <see langword="false"/> when the name is defined by
        /// neither, letting the caller raise the error with a source line number attached.
        /// </summary>
        public bool TryGetAttribute(string name, out SourceValue value)
        {
            if (_fields.TryGetValue(name, out value))
            {
                return true;
            }

            if (!Class.TryGetAttribute(name, out var classAttribute))
            {
                return false;
            }

            // Bound fresh on every access rather than cached, because class attributes stay mutable
            // after the class is built — a cached binding would survive a later rebind of the name.
            value = classAttribute.ToObject() is SourceFunction method
                ? new SourceValue(method.Bind(new SourceValue(this))) : classAttribute;

            return true;
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
            // Writes always land on the instance, so assigning a name a class variable already holds
            // shadows it here and leaves the class untouched.
            _fields[name.ToString()] = value;
        }

        public override List<string> Directory
        {
            get
            {
                var names = new List<string>(_fields.Keys);

                foreach (var classAttrName in Class.Directory)
                {
                    if (!_fields.ContainsKey(classAttrName))
                    {
                        names.Add(classAttrName);
                    }
                }

                return names;
            }
        }

        public override string ToRepresentation()
        {
            return string.Format(RepresentationFormat, Class.Name);
        }
    }
}
