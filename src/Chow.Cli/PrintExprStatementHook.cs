using Chow.Interpreter.Hooks;
using Chow.Interpreter.Values;

namespace Chow.Repl
{
    sealed class PrintExprStatementHook : IExpressionStatementHook
    {
        public void Invoke(object value)
        {
            Console.WriteLine((ChowValue)value);
        }
    }
}
