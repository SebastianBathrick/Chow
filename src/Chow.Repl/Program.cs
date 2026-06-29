namespace Chow.Repl;

static class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 1)
        {
            return TryRunCommand(args[0]) ? 0 : 1;
        }
        
        var repl = new ReadEvalPrintLooper();
        repl.Loop();
        return 0;
    }

    static bool TryRunCommand(string command)
    {
        if (command == "--help")
        {
            ReadEvalPrintLooper.PrintHelp();
            return true;
        }

        Console.WriteLine(
            $"INVALID COMMAND: 'command' is not a valid command. "
            + $"Use the '--help' command to print a list of valid commands.");
        return false;
    }
}
