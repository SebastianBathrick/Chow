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
            return IsTruthy(ref l) ? r : l;
        }

        public static TaggedUnion EvaluateOr(ref TaggedUnion l, ref TaggedUnion r)
        {
            return IsTruthy(ref l) ? l : r;
        }

        #endregion

        #region Unary Operations

        public static TaggedUnion EvaluateNot(ref TaggedUnion operand)
        {
            return new TaggedUnion(!IsTruthy(ref operand));
        }

        #endregion

        #region Truthiness Helpers

        public static bool IsTruthy(ref TaggedUnion operand)
        {
            return operand.ToBool();
        }

        public static bool ShouldShortCircuitAnd(ref TaggedUnion operand)
        {
            return !IsTruthy(ref operand);
        }

        public static bool ShouldShortCircuitOr(ref TaggedUnion operand)
        {
            return IsTruthy(ref operand);
        }

        #endregion
    }
}
