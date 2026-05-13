using Chow.Interpreter;
using Chow.Interpreter.Values;

namespace CodeEditor;

static class Program
{
    public const string RunWorkerArgument = "--run-chow-worker";

    [STAThread]
    static int Main(string[] args)
    {
        if (args.Length == 2 && args[0] == RunWorkerArgument)
        {
            return RunChowWorker(args[1]);
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
        return 0;
    }

    static int RunChowWorker(string filePath)
    {
        try
        {
            var source = File.ReadAllText(filePath);
            var module = new ChowModule
            {
                ["print"] = (Action<ChowValue>)(value => Console.WriteLine(value)),
                ["input"] = (Func<ChowValue>)(() =>
                    throw new InvalidOperationException("input() is not supported in CodeEditor."))
            };

            module.Execute(source);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }
}
