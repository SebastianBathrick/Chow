using Chow.Interpreter;
using System.Diagnostics;
using ReplLoop = Chow.Repl.Repl;


namespace Chow.Cli
{
    sealed class CliApp
    {
        readonly ChowModule _module;

        public CliApp(ChowModule module)
        {
            _module = module ?? throw new ArgumentNullException(nameof(module));
        }
        
        public int Run(string[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (args.Length == 0)
            {
                var repl = new ReplLoop(_module);
                return repl.Run();
            }

            if (args.Length > 1)
            {
                var stopwatch = Stopwatch.StartNew();
                var exitCode = ExecuteFile(args[0]);
                stopwatch.Stop();

                Console.Error.WriteLine($"Execution time: {stopwatch.ElapsedMilliseconds} ms");
                return exitCode;
            }

            return ExecuteFile(args[0]);
        }

        int ExecuteFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                Console.Error.WriteLine("A Chow source file path is required.");
                return ExitCodes.UsageError;
            }

            try
            {
                var sourceCode = File.ReadAllText(filePath);
                _module.Execute(sourceCode);
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
