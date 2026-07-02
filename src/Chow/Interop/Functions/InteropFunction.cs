using Chow.Interop.Functions.Interfaces;
using Chow.SourceData;

/*
    InteropFunction (abstract)
     │   Stores: Arity, MinArguments, MaxArguments, EnclosingScope, CallType (Native)
     │   Constructor: takes scope(optional/null) (+ arity info) and stores it — this is the only
     │   place any state lives in the entire tree
     │
     ├── NativeVoidFunction : InteropFunction
     │     InvokeOverload => Void
     │     abstract void Invoke()
     │     (no fields of its own)
     │
     ├── NativeReturnValueFunction : InteropFunction
     │     InvokeOverload => ReturnValue
     │     abstract SourceValue Invoke()
     │     (no fields of its own)
     │
     ├── NativeArgsAndVoidFunction : InteropFunction
     │     InvokeOverload => ArgsAndVoid
     │     abstract void Invoke(SourceValue[] args)
     │     (no fields of its own)
     │
     └── NativeArgsAndReturnValueFunction : InteropFunction
           InvokeOverload => ArgsAndReturnValue
           abstract SourceValue Invoke(SourceValue[] args)
           (no fields of its own)

    Leaf class examples:
     PrintFunction : NativeArgsAndVoidFunction
       PrintFunction(SourceValue scope) : base(scope) { }
       override void Invoke(SourceValue[] args) => Console.WriteLine(...);
       (no fields, no extra logic — just the constructor pass-through and Invoke body)
 */

namespace Chow.Interop.Functions
{
   abstract class InteropFunction : IInteropFunction
   {
       const int DefaultMinArguments = 0;
       const int DefaultMaxArguments = 0;

       public abstract FunctionType FunctionType
       {
           get;
       }

       
       public abstract Arity Arity
       {
           get;
       }
       
       public SourceValue EnclosingScope => SourceValue.None;

       public int MinArguments => DefaultMinArguments;

       public int MaxArguments => DefaultMaxArguments;
       
       public void Call
   }
}
