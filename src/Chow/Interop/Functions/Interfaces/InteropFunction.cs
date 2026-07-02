using Chow.SourceData;

namespace Chow.Interop.Functions.Interfaces
{
    interface InteropFunction
    {
        ISourceObject Scope { get; }
        
        int MinimumArguments { get; }
        
        int MaximumArguments { get; }
    }
}
