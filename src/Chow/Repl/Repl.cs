using Chow.Cli;
using Chow.Execution;
namespace Chow.Repl
{
    /// <summary>
    /// Runs the interactive Chow read-evaluate-print loop.
    /// </summary>
    internal sealed class Repl
    {
        readonly IChowExecutor _executor;
        readonly ConsoleLineEditor _lineEditor;
        readonly PromptStyle _promptStyle;

        /// <summary>
        /// Initializes a REPL using the default console line editor and prompt style.
        /// </summary>
        /// <param name="executor">Executes submitted Chow source code.</param>
        public Repl(IChowExecutor executor)
            : this(executor, new ConsoleLineEditor(), PromptStyle.Default)
        {
        }

        /// <summary>
        /// Initializes a REPL with explicit input and prompt dependencies.
        /// </summary>
        /// <param name="executor">Executes submitted Chow source code.</param>
        /// <param name="lineEditor">Reads interactive user submissions.</param>
        /// <param name="promptStyle">Defines the prompt text displayed by the REPL.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="executor"/> or <paramref name="lineEditor"/> is <see langword="null"/>.
        /// </exception>
        public Repl(IChowExecutor executor, ConsoleLineEditor lineEditor, PromptStyle promptStyle)
        {
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
            _lineEditor = lineEditor ?? throw new ArgumentNullException(nameof(lineEditor));
            _promptStyle = promptStyle;
        }

        /// <summary>
        /// Starts the REPL and blocks until input reaches EOF or the user requests exit.
        /// </summary>
        /// <returns>An exit code from <see cref="ExitCodes"/>.</returns>
        public int Run()
        {
            var exitRequested = false;

            ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
            {
                // Cancel the default process termination so the loop can leave cleanly.
                eventArgs.Cancel = true;
                exitRequested = true;
                Console.Error.WriteLine();
            };

            Console.CancelKeyPress += cancelHandler;

            try
            {
                if (ShouldUseReadLineMode())
                {
                    // Redirected streams cannot use ReadKey or cursor positioning safely.
                    return RunReadLineMode(() => exitRequested);
                }

                return RunInteractiveMode(() => exitRequested);
            }
            finally
            {
                Console.CancelKeyPress -= cancelHandler;
            }
        }

        int RunInteractiveMode(Func<bool> isExitRequested)
        {
            var previousControlCMode = Console.TreatControlCAsInput;

            try
            {
                // Let the editor consume Ctrl+C as input so it can cancel the current submission.
                Console.TreatControlCAsInput = true;

                while (!isExitRequested())
                {
                    var sourceCode = _lineEditor.ReadSubmission(_promptStyle);
                    if (sourceCode == null)
                    {
                        return ExitCodes.Success;
                    }

                    ExecuteIfNotBlank(sourceCode);
                }

                return ExitCodes.Success;
            }
            finally
            {
                Console.TreatControlCAsInput = previousControlCMode;
            }
        }

        int RunReadLineMode(Func<bool> isExitRequested)
        {
            var shouldWritePrompt = !Console.IsInputRedirected && !Console.IsOutputRedirected;

            while (!isExitRequested())
            {
                if (shouldWritePrompt)
                {
                    Console.Write(_promptStyle.StartIndicator);
                }

                var sourceCode = Console.ReadLine();
                if (sourceCode == null)
                {
                    return ExitCodes.Success;
                }

                ExecuteIfNotBlank(sourceCode);
            }

            return ExitCodes.Success;
        }

        void ExecuteIfNotBlank(string sourceCode)
        {
            if (string.IsNullOrWhiteSpace(sourceCode))
            {
                return;
            }

            try
            {
                _executor.Execute(sourceCode);
            }
            catch (Exception ex) when (!ExceptionPolicy.IsFatal(ex))
            {
                ExceptionPolicy.WriteError(ex);
            }
        }

        bool ShouldUseReadLineMode()
        {
            if (Console.IsInputRedirected || Console.IsOutputRedirected)
            {
                return true;
            }

            try
            {
                // The editor needs at least one printable column after the prompt.
                return Console.BufferWidth <= _promptStyle.IndicatorLength + 1;
            }
            catch (IOException)
            {
                return true;
            }
        }
    }
}
