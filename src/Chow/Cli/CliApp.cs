using Chow.Execution;
using ReplLoop = Chow.Repl.Repl;

namespace Chow.Cli
{
    /// <summary>
    /// Dispatches command-line input to the appropriate Chow CLI behavior.
    /// </summary>
    sealed class CliApp
    {
        readonly IChowExecutor _executor;

        /// <summary>
        /// Initializes a new CLI app using the executor shared by CLI modes.
        /// </summary>
        /// <param name="executor">Executes Chow source code for REPL and inline-source modes.</param>
        /// <exception cref="ArgumentNullException"><paramref name="executor"/> is <see langword="null"/>.</exception>
        public CliApp(IChowExecutor executor)
        {
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        }

        /// <summary>
        /// Runs the CLI: no arguments starts the REPL, and the first argument is treated as inline Chow source.
        /// </summary>
        /// <param name="args">Command-line arguments passed to the process.</param>
        /// <returns>An exit code from <see cref="ExitCodes"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> is <see langword="null"/>.</exception>
        public int Run(string[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (args.Length == 0)
            {
                var repl = new ReplLoop(_executor);
                return repl.Run();
            }

            // File-path handling intentionally lives outside this dispatch path until the future file/module system exists.
            return ExecuteInlineSource(args[0]);
        }

        int ExecuteInlineSource(string sourceCode)
        {
            if (string.IsNullOrWhiteSpace(sourceCode))
            {
                return ExitCodes.Success;
            }

            try
            {
                _executor.Execute(sourceCode);
                return ExitCodes.Success;
            }
            catch (Exception ex) when (!ExceptionPolicy.IsFatal(ex))
            {
                ExceptionPolicy.WriteError(ex);
                return ExitCodes.RuntimeError;
            }
        }
    }
}
