using Chow.DataTypes;

namespace Chow.Expressions
{
    /// <summary>
    /// Static service class for <see cref="Chow.Core.VirtualMachine"/> that performs logical
    /// operations that behave the same as Python's logical operators.
    /// </summary>
    static class LogicEvaluator
    {
        #region Binary Operations

        public static TaggedUnion EvaluateAnd(ref TaggedUnion l, ref TaggedUnion r)
        {
            // Python `and` returns an operand value, not a coerced bool.
            return IsTruthy(ref l) ? r : l;
        }

        public static TaggedUnion EvaluateOr(ref TaggedUnion l, ref TaggedUnion r)
        {
            // Python `or` returns the first truthy operand, otherwise the right operand.
            return IsTruthy(ref l) ? l : r;
        }

        #endregion

        #region Unary Operations

        public static TaggedUnion EvaluateNot(ref TaggedUnion operand)
        {
            // Unlike `and`/`or`, Python `not` always produces an actual bool.
            return new TaggedUnion(!IsTruthy(ref operand));
        }

        #endregion

        #region Truthiness Helpers

        public static bool IsTruthy(ref TaggedUnion operand)
        {
            // Keep truthiness centralized on TaggedUnion's Python-style conversion rules.
            return operand.ToBool();
        }

        public static bool ShouldShortCircuitAnd(ref TaggedUnion operand)
        {
            // `and` stops at the first falsy value and leaves that value on the stack.
            return !IsTruthy(ref operand);
        }

        public static bool ShouldShortCircuitOr(ref TaggedUnion operand)
        {
            // `or` stops at the first truthy value and leaves that value on the stack.
            return IsTruthy(ref operand);
        }

        #endregion
    }
}
