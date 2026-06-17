namespace Chow
{
    /// <summary>
    /// The common interface shared by every Chow object type, such as <see cref="ChowObject"/>,
    /// <see cref="ChowList"/>, <see cref="ChowDictionary"/>, and <see cref="ChowScope"/>.
    /// </summary>
    public interface IChowObject
    {
        /// <summary>Returns the Chow string representation of this object.</summary>
        /// <returns>The string representation of this object.</returns>
        string ToString();
    }
}
