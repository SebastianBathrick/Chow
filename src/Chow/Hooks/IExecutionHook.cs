using Chow.Interpreter.Values;

namespace Chow.Interpreter.Hooks
{
    public interface IExecutionHook
    {
        void Invoke(ChowValue value);
    }
}
