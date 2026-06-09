using Chow.Objects;
namespace Chow.VM.Utilities
{
    /// <summary>
    /// Static service class for <see cref="Processor"/> that performs logical
    /// operations that behave the same as Python's logical operators.
    /// </summary>
    static class LogicEvaluator
    {
        #region Binary Operations

        public static SourceValue EvaluateAnd(SourceValue r, SourceValue l)
        {
            // Python `and` returns an operand value, not a coerced bool.
            return IsTruthy(ref l) ? r : l;
        }

        public static SourceValue EvaluateOr(SourceValue r, SourceValue l)
        {
            // Python `or` returns the first truthy operand, otherwise the right operand.
            return IsTruthy(ref l) ? l : r;
        }

        #endregion

        #region Unary Operations

        public static SourceValue EvaluateNot(SourceValue operand)
        {
            // Unlike `and`/`or`, Python `not` always produces an actual bool.
            return new SourceValue(!IsTruthy(ref operand));
        }

        #endregion

        #region Truthiness Helpers

        public static bool IsTruthy(ref SourceValue operand)
        {
            // Keep truthiness centralized on SourceValue's Python-style conversion rules.
            return operand.ToBool();
        }

        public static bool ShouldShortCircuitAnd(ref SourceValue operand)
        {
            // `and` stops at the first falsy value and leaves that value on the stack.
            return !IsTruthy(ref operand);
        }

        public static bool ShouldShortCircuitOr(ref SourceValue operand)
        {
            // `or` stops at the first truthy value and leaves that value on the stack.
            return IsTruthy(ref operand);
        }

        #endregion
    }
}
