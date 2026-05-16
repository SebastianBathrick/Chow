using System.Diagnostics;
using Chow.Interpreter;
namespace CodeEditor;

static class Program
{
    public const string RunWorkerArgument = "--run-chow-worker";
    public const string DebugDurationArgument = "--debug-duration";
    public const string ExecutionDurationPrefix = "__CODEEDITOR_EXECUTION_MS__:";

    [STAThread]
    static int Main(string[] args)
    {
        if (args.Length >= 2 && args[0] == RunWorkerArgument)
        {
            var includeExecutionDuration = args.Length >= 3 && args[2] == DebugDurationArgument;
            return RunChowWorker(args[1], includeExecutionDuration);
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
        return 0;
    }

    static int RunChowWorker(string filePath, bool includeExecutionDuration)
    {
        try
        {
            var source = File.ReadAllText(filePath);
            var module = new ChowModule();

            var stopwatch = Stopwatch.StartNew();
            try
            {
                module.Execute(source);
                stopwatch.Stop();
                WriteExecutionDuration(includeExecutionDuration, stopwatch.ElapsedMilliseconds);
                return 0;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Console.Error.WriteLine(ex);
                WriteExecutionDuration(includeExecutionDuration, stopwatch.ElapsedMilliseconds);
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    static void WriteExecutionDuration(bool includeExecutionDuration, long elapsedMilliseconds)
    {
        if (includeExecutionDuration)
        {
            Console.Error.WriteLine($"{ExecutionDurationPrefix}{elapsedMilliseconds}");
        }
    }
}
