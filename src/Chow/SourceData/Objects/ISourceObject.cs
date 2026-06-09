using System.Collections.Generic;

namespace Chow.SourceData
{
    interface ISourceObject
    {
        // ---------------- Type ----------------

        DataType Type { get; }

        // ---------------- String representations ----------------

        string ToRepresentation();

        // ---------------- Truthiness & length ----------------

        bool Truthiness { get; }
        int Length { get; }

        // ---------------- Attribute protocol ----------------

        SourceValue GetAttribute(SourceValue name);
        void SetAttribute(SourceValue name, SourceValue value);
        void DeleteAttribute(SourceValue name);
        List<string> Directory { get; }

        // ---------------- Item (container) protocol ----------------

        SourceValue GetItem(SourceValue key);
        void SetItem(SourceValue key, SourceValue value);
        void DeleteItem(SourceValue key);

        // ---------------- Membership & iteration ----------------

        bool Contains(SourceValue value);
        IIterator GetIterator();
        IIterator GetReversedIterator();

        // ---------------- Equality & hashing ----------------

        bool EqualsTo(SourceObject other);
        int HashCode();

        // ---------------- Callability ----------------

        SourceValue Call(params SourceValue[] args);
    }
}
