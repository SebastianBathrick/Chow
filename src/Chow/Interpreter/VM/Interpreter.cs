using Chow.SourceData;

namespace Chow.Interpreter.VM
{
    static class Interpreter
    {
        public static void Run(string srcCode, Scope globalScope, out SourceValue result)
        {
            var chunk = BytecodeConverter.Compile(srcCode);
            VirtualMachine.RunChunk(chunk, globalScope, out result);
        }

        public static void RunFunctionCall(ref SourceValue func, SourceValue[] args, out SourceValue returnVal)
        {
            VirtualMachine.RunFunctionCall(ref func, out returnVal, args);
        }
    }
}
