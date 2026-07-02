using Chow.SourceData;

namespace Chow.Interop.Functions.Interfaces
{
    interface IInteropFunction
    {
        Arity Arity { get; }
        
        FunctionType FunctionType { get; }
        
        
        
        int MinArguments { get; }
        
        int MaxArguments { get; }
        
        SourceValue EnclosingScope { get; }
    }
}
