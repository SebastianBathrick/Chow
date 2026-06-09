using System;

namespace Chow.Objects
{
    /// <summary>
    /// Low-level immutable representation of a Python-style <c>range</c>. Stores the start, stop,
    /// and step values and produces values on demand via <see cref="GetIterator"/>.
    /// </summary>
    class SourceRange
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

        public IIterator GetIterator()
        {
            return new SourceRangeIterator(this);
        }

        public override string ToString()
        {
            return Step == 1 ? $"range({Start}, {Stop})" : $"range({Start}, {Stop}, {Step})";

        }
    }
}
