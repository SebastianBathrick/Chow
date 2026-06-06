namespace Chow.Expressions
{
    /// <summary>
    /// Represents a service that provides a public API to evaluate <see cref="RuntimeValue"/>
    /// instances and return the evaluated results.
    /// </summary>
    public interface IEvaluator
    {
        RuntimeValue EvaluateBinary(RuntimeValue right, RuntimeValue left);
    }
}
