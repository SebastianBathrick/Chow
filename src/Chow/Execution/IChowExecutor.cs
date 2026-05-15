namespace Chow.Execution
{
    /// <summary>
    /// Executes Chow source code for CLI features without exposing the REPL to interpreter API details.
    /// </summary>
    internal interface IChowExecutor
    {
        /// <summary>
        /// Executes a single Chow source-code submission.
        /// </summary>
        /// <param name="sourceCode">The Chow source code to execute.</param>
        void Execute(string sourceCode);
    }
}
