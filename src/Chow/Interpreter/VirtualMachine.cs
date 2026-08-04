using System;
using Chow.Bytecode;
using Chow.Interpreter.VM;
using Chow.SourceData;

namespace Chow.Interpreter
{
    static class VirtualMachine
    {
        public static void Run(string srcCode, Scope globalScope, out SourceValue result)
        {
            var chunk = BytecodeConverter.Compile(srcCode);
            RunChunk(chunk, globalScope, out result);
        }

        static void RunChunk(BytecodeChunk chunk, Scope globalScope, out SourceValue result)
        {
            var processor = new Processor(globalScope, chunk);
            
            // Returns the result of the last expression statement to execute or SourceValue.None
            result = processor.Execute();
        }
        
        public static void RunFunctionCall(ref SourceValue func, out SourceValue returnVal, SourceValue[] args)
        {
            // Built-in methods (list.append and friends) are plain delegates, so they are invoked
            // directly instead of paying to stand up a Processor.
            if (func.ToObject() is Func<SourceValue[], SourceValue> hostDelegate)
            {
                returnVal = hostDelegate.Invoke(args ?? Array.Empty<SourceValue>());
                return;
            }

            // Everything else — Chow closures, bound methods, classes, other host delegate shapes —
            // is run by a Processor, which applies the same call rules compiled code goes through.
            var processor = new Processor(FindModuleScope(ref func));
            returnVal = processor.CallValue(func, args);
        }

        /// <summary>
        /// Finds the module scope a Chow callable was defined under by walking its captured scope
        /// chain to the root. A call made from the host has no surrounding frame to inherit one
        /// from, and it is what <c>global</c> inside the body resolves against.
        /// </summary>
        /// <returns>
        /// The module scope, or <c>null</c> for callees that carry no scope, such as host delegates.
        /// </returns>
        static Scope FindModuleScope(ref SourceValue func)
        {
            var scope = (func.ToObject() as SourceFunction)?.Enclosing;

            while (scope?.Parent != null)
            {
                scope = scope.Parent;
            }

            return scope;
        }
    }
}
