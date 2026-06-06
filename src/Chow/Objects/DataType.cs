namespace Chow.DataTypes
{
    // TODO: Change this back to internal after converting TaggedUnion to internal
    public enum DataType : byte
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
