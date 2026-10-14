using Chow.Bytecode;

namespace Chow
{
    /// <summary>
    /// A piece of Chow source code that has already been compiled, created by
    /// <see cref="ChowEngine.Compile"/> and executed by
    /// <see cref="ChowEngine.Run(ChowProgram, IChowObject, bool)"/>.
    /// <para>
    /// Compiling once and running the program many times skips the cost of compiling the source code
    /// on every run. A program is not tied to any scope, so the same program can run against
    /// different scopes, and running it never changes the program itself. It is safe to run one
    /// program from multiple threads at once, provided each concurrent run uses its own scope.
    /// </para>
    /// </summary>
    public sealed class ChowProgram
    {
        internal BytecodeChunk BytecodeChunk { get; }

        internal ChowProgram(BytecodeChunk bytecodeChunk)
        {
            BytecodeChunk = bytecodeChunk;
        }
    }
}
