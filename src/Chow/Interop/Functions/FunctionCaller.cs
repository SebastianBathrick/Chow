using System;
using Chow.Interop.Functions.Interfaces;
using Chow.SourceData;
using Chow.Utility;
using Chow.VM;

namespace Chow.Interop.Functions
{
    static class FunctionCaller
    {
        // Processor calls this first to see if it will have to execute the bytecode for the call.
        public static bool IsChunkCall(ref SourceValue func)
        {
            return func.DataType == DataType.Function 
                && ((IInteropFunction)func.ToObject()).FunctionType == FunctionType.Chunk;
        }

        public static void Call(
            ref SourceValue func, 
            out SourceObject returnVal, 
            SourceValue[] args = null)
        {
            if (func.DataType != DataType.Function)
            {
                var typeName = DataTypeNames.GetTypeName(func.DataType);
                throw new DataTypeException($"TypeError: '{typeName}' object is not callable");
            }
            
            // It is always assumed that the source value will be an IInteropFunction instance
            var interopFunc = (IInteropFunction)func.ToObject();

            switch (interopFunc.FunctionType)
            {
                case FunctionType.Native:
                    CallNative(interopFunc, out returnVal, args);
                    return;
                // TODO: Add additional call types.
                default:
                    throw new UnreachableException(nameof(Call));
            }
        }

        static void CallNative(IInteropFunction func, out SourceObject returnVal, SourceValue[] args = null)
        {
            
        }
    }
}
