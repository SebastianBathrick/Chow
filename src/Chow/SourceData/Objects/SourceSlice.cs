namespace Chow.SourceData
{
    /// <summary>
    /// Bundles slice bounds for the item protocol, so a slice expression is just
    /// <c>GetItem(slice)</c>. (Python: <c>slice</c>) Any bound may be <see cref="SourceValue.None"/>.
    /// </summary>
    sealed class SourceSlice : SourceObject
    {
        public override DataType Type => DataType.Slice;

        public SourceValue Start { get; }
        public SourceValue Stop { get; }
        public SourceValue Step { get; }

        public SourceSlice(SourceValue start, SourceValue stop, SourceValue step)
        {
            Start = start;
            Stop = stop;
            Step = step;
        }

        public override string ToRepresentation()
        {
            return $"slice({Start}, {Stop}, {Step})";
        }
    }
}
