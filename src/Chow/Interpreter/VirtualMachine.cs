using System;
using System.Runtime.CompilerServices;
using Chow.Bytecode;
using Chow.Interpreter.Exceptions;
using Chow.Interpreter.VM;
using Chow.SourceData;

namespace Chow.Interpreter
{
    static class VirtualMachine
    {
        const string HostStackExhaustedMessage =
            "maximum recursion depth exceeded while calling into Chow from the host";

        public static BytecodeChunk Compile(string srcCode)
        {
            return BytecodeConverter.Compile(srcCode);
        }

        public static void Run(BytecodeChunk chunk, Scope globalScope, out SourceValue result)
        {
            ThrowIfHostStackExhausted();
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
            ThrowIfHostStackExhausted();
            var processor = new Processor(FindModuleScope(ref func));
            returnVal = processor.CallValue(func, args);
        }

        /// <summary>
        /// Throws a <see cref="RecursionException"/> when the C# stack is close to overflowing. Chow
        /// calls don't use the C# stack, but a host callback that calls back into Chow nests a new
        /// Processor on it each time, and an overflow would end the host's process instead of raising
        /// an error. The recursion limit can't rule this out, because the stack's size depends on the
        /// host's thread.
        /// </summary>
        static void ThrowIfHostStackExhausted()
        {
            try
            {
                RuntimeHelpers.EnsureSufficientExecutionStack();
            }
            catch (InsufficientExecutionStackException)
            {
                throw new RecursionException(HostStackExhaustedMessage);
            }
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
