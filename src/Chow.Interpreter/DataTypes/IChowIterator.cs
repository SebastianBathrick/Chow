namespace Chow.Interpreter.DataTypes
{
    /// <summary>
    /// Internal iteration protocol used by the virtual machine to drive Chow-language <c>for</c> loops.
    /// Implementations advance one step per call and signal exhaustion via the return value.
    /// </summary>
    /// <remarks>
    /// Combining "advance" and "fetch current" into a single call simplifies the bytecode contract:
    /// the for-loop dispatcher only needs one opcode that pushes the next value or jumps past the body.
    /// </remarks>
    interface IChowIterator
    {
        /// <summary>Advances and produces the next element.</summary>
        /// <param name="current">The next element when the call returns <see langword="true"/>; otherwise undefined.</param>
        /// <returns><see langword="true"/> if an element was produced; <see langword="false"/> when the iterator is exhausted.</returns>
        bool TryMoveNext(out ChowValue current);
    }
}
