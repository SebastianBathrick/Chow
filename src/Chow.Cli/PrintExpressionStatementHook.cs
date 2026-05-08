using Chow.Interpreter.Hooks;
using Chow.Interpreter.Values;

namespace Chow.Repl
{
    internal sealed class PrintExpressionStatementHook : IExpressionStatementHook
    {
        public void Invoke(ChowValue value)
        {
            Console.WriteLine(value);
        }
    }
}
