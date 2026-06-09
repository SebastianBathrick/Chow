using System;
namespace Chow.SourceData
{
    /// <summary>
    /// Low-level immutable representation of a Python-style <c>range</c>. Stores the start, stop,
    /// and step values and produces values on demand via <see cref="GetIterator"/>.
    /// </summary>
    class SourceRange : SourceObject
    {
        public long Start { get; }
        public long Stop { get; }
        public long Step { get; }

        public SourceRange(long start, long stop, long step)
        {
            if (step == 0)
            {
                throw new ArgumentException("range() arg 3 must not be zero");
            }

            Start = start;
            Stop = stop;
            Step = step;
        }

        public override DataType Type => DataType.Range;

        public override bool HasLength => true;

        public override int Length => Count;

        public int Count
        {
            get
            {
                long span;

                if (Step > 0)
                {
                    span = Stop - Start;
                }
                else
                {
                    span = Start - Stop;
                }

                if (span <= 0)
                {
                    return 0;
                }

                var absStep = Step > 0 ? Step : -Step;
                return (int)((span + absStep - 1) / absStep);
            }
        }

        public override IIterator GetIterator()
        {
            return new SourceRangeIterator(this);
        }

        public override string ToRepresentation()
        {
            return Step == 1 ? $"range({Start}, {Stop})" : $"range({Start}, {Stop}, {Step})";
        }
    }
}
