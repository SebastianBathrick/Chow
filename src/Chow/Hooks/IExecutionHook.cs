using Chow.Interpreter.Values;
using System;

namespace Chow.Interpreter.Hooks
{
    public interface IExecutionHook
    {
        void Invoke(ChowValue value = null);
    }
}
