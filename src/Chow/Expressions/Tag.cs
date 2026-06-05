namespace Chow.DataTypes
{
    // TODO: Change this back to internal after converting TaggedUnion to internal
    public enum Tag : byte
    {
        None,
        Bool,
        Object,
        Long,
        Double,
        Str,
        List,
        Dict,
        Range
    }
}
