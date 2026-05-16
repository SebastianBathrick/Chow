using Chow.Interpreter;
namespace Chow.Execution
{
    /// <summary>
    /// Adapts <see cref="ChowModule"/> to the CLI executor interface.
    /// </summary>
    internal sealed class ChowModuleExecutor : IChowExecutor
    {
        readonly ChowModule _module = new ChowModule();

        /// <inheritdoc />
        public void Execute(string sourceCode)
        {
            _module.Execute(sourceCode);
        }
    }
}
