using System;
using Chow.Bytecode;
using Chow.Interpreter.Exceptions;
using Chow.Interpreter.VM;
using Chow.SourceData;

namespace Chow.Interpreter
{
    static class VirtualMachine
    {
        public static void RunChunk(BytecodeChunk chunk, Scope globalScope, out SourceValue result)
        {
            var processor = new Processor(globalScope, chunk);
            
            // Returns the result of the last expression statement to execute or SourceValue.None
            result = processor.Execute();
        }
        
        public static void RunFunctionCall(ref SourceValue func, out SourceValue returnVal, SourceValue[] args)
        {
            if (func.DataType != DataType.Object)
            {
                throw new UnreachableException(
                    $"{nameof(RunFunctionCall)} call with invalid data type '{func.DataType}'");
            }

            var delegateFunc = (Func<SourceValue[], SourceValue>)func.ToObject();

            returnVal = delegateFunc.Invoke(args);
        }
    }
}
