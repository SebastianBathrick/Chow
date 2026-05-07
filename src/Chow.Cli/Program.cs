using Chow.Cli;
using Chow.Interpreter;
using Chow.Interpreter.Values;

Console.TreatControlCAsInput = true;

while (CommandLineEditor.TryReadBlock(out string sourceCode))
{
    try
    {
        ChowInstance instance = new ChowInstance();
        ChowValue returnValue = instance.Run(sourceCode);
        Console.WriteLine(returnValue);
        Console.WriteLine(instance.GetVariableDebugInfo());
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }
}

return 0;
