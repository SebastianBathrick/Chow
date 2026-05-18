using Chow.Cli;
using Chow.Interpreter;
namespace Chow.Repl
{
    sealed class Repl
    {
        readonly ChowModule _module;
        readonly ConsoleLineEditor _lineEditor;
        readonly PromptStyle _promptStyle;

        public Repl(ChowModule module)
            : this(module, new ConsoleLineEditor(), PromptStyle.Default)
        {
        }

        public Repl(ChowModule module, ConsoleLineEditor lineEditor, PromptStyle promptStyle)
        {
            _module = module ?? throw new ArgumentNullException(nameof(module));
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
                _module.Execute(sourceCode);
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
