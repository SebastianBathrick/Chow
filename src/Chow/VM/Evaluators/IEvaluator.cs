using Chow.Objects;
namespace Chow.VM.Utilities
{
    /// <summary>
    /// Represents a service that provides a public API to evaluate <see cref="SourceValue"/>
    /// instances and return the evaluated results.
    /// </summary>
    public interface IEvaluator
    {
        SourceValue EvaluateBinary(SourceValue right, SourceValue left);
    }
}
