using Chow.Interpreter.Hooks;
using Chow.Interpreter.Values;

namespace Chow.Repl
{
    internal sealed class PrintExprStatementHook : IExprStatementHook
    {
        public void Invoke(ChowValue value)
        {
            Console.WriteLine(value);
        }
    }
}
