namespace Chow
{
    /// <summary>
    /// The common interface shared by every Chow object type, such as <see cref="ChowObject"/>,
    /// <see cref="ChowList"/>, <see cref="ChowDict"/>, and <see cref="ChowScope"/>.
    /// </summary>
    public interface IChowObject
    {
        /// <summary>Whether this object is the Chow <c>None</c> object.</summary>
        bool IsNone { get; }

        /// <summary>Whether this object is a Chow <c>list</c>.</summary>
        bool IsList { get; }

        /// <summary>Whether this object is a Chow <c>dict</c>.</summary>
        bool IsDictionary { get; }

        /// <summary>Whether this object is a Chow scope.</summary>
        bool IsScope { get; }

        /// <summary>Whether this object is a Chow <c>str</c>.</summary>
        bool IsString { get; }

        /// <summary>
        /// Whether this object is a Chow class, as produced by a <c>class</c> declaration. See
        /// <see cref="ChowObject.ClassName"/> to identify which one.
        /// </summary>
        bool IsClass { get; }

        /// <summary>
        /// Whether this object is an instance of a Chow class. See
        /// <see cref="ChowObject.ClassName"/> to identify the class it was created from.
        /// </summary>
        bool IsClassInstance { get; }

        /// <summary>Returns the Chow string representation of this object.</summary>
        /// <returns>The string representation of this object.</returns>
        string ToString();
    }
}
