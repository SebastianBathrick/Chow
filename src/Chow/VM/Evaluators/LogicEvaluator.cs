using Chow.Objects;
using Chow.Utility;

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

        public static SourceValue EvaluateUnion(SourceValue r, SourceValue l)
        {
            // Among Chow's current types, Python only defines `|` for dicts (PEP 584 merge).
            if (l.DataType == DataType.Dict && r.DataType == DataType.Dict)
            {
                return new SourceValue(
                    SourceDictionary.Merge((SourceDictionary)l.ToObject(), (SourceDictionary)r.ToObject()));
            }

            throw UnsupportedBinary(l.DataType, r.DataType, ExpressionOperator.BinaryOr);
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

        #region Error Helpers

        static DataTypeException UnsupportedBinary(DataType leftDataType, DataType rightDataType, ExpressionOperator op)
        {
            // Message shape mirrors Python's TypeError wording and type names.
            return new DataTypeException(
                $"TypeError: unsupported operand type(s) for {OperatorStrings.ToSource(op)}: "
                + $"'{DataTypeNames.GetTypeName(leftDataType)}' and '{DataTypeNames.GetTypeName(rightDataType)}'");
        }

        #endregion
    }
}
