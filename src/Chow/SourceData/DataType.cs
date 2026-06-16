namespace Chow.SourceData
{
    // TODO: Change this back to internal after converting SourceValue to internal
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
        Range,
        Function,
        Slice,
        Scope
    }
}
