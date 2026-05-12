using Chow.Interpreter.Values;
using System;

namespace Chow.Interpreter.Hooks
{
    public interface IHook
    {
        void Invoke(object value = null);
    }
}
