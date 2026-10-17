using System;
using Chow.Code;
using Chow.Interpreter.Exceptions;
using Chow.SourceData;
using Chow.Syntax;

namespace Chow.Interpreter.Compilation
{
    /// <summary>
    /// Decides whether an expression is a compile-time constant the way CPython 3.11's AST optimizer
    /// (<c>ast_opt.c</c>) does, and computes its value. Only used where CPython's choice is observable:
    /// a set display of constants compiles to a frozenset constant, which changes the set's layout.
    /// </summary>
    static class ConstantFolder
    {
        // ast_opt.c limits: results past these are left for runtime rather than folded.
        const int MaxIntBits = 128; // MAX_INT_SIZE
        const int MaxStrLength = 4096; // MAX_STR_SIZE

        public static bool TryFold(Node node, out SourceValue value)
        {
            switch (node)
            {
                case LiteralNode literal:
                    value = LiteralValue(literal);
                    return true;
                case ExpressionNode expression when expression.Right == null:
                    return TryFoldUnary(expression, out value);
                case ExpressionNode expression:
                    return TryFoldBinary(expression, out value);
                default:
                    value = SourceValue.None;
                    return false;
            }
        }

        public static SourceValue LiteralValue(LiteralNode literalNode)
        {
            // Cases for LiteralNodeType where the boxed Value is the wrong CLR type should not occur unless the Parser is bugged
            switch (literalNode.Type)
            {
                case LiteralNodeType.Integer:
                    if (literalNode.Value is long intVal)
                    {
                        return new SourceValue(intVal);
                    }

                    break;
                case LiteralNodeType.Float:
                    if (literalNode.Value is double floatVal)
                    {
                        return new SourceValue(floatVal);
                    }

                    break;

                case LiteralNodeType.Boolean:
                    if (literalNode.Value is bool boolVal)
                    {
                        return new SourceValue(boolVal);
                    }

                    break;
                case LiteralNodeType.None:
                    return SourceValue.None;
                case LiteralNodeType.String:
                    if (literalNode.Value is string strVal)
                    {
                        return new SourceValue(strVal);
                    }

                    break;
                default:
                    throw new NotImplementedException(
                        $"Compilation of literal type {literalNode.Type} is not implemented.");
            }

            // Reached only when the boxed Value's CLR type doesn't match its declared LiteralNodeType (parser bug).
            throw new InvalidOperationException();
        }

        // fold_unaryop: `-` and `not` of a constant. A failure (such as `-'a'`) means it isn't folded.
        static bool TryFoldUnary(ExpressionNode expression, out SourceValue value)
        {
            value = SourceValue.None;

            if (!TryFold(expression.Left, out var operand))
            {
                return false;
            }

            switch (expression.Operator)
            {
                case Operator.Negate:
                    try
                    {
                        value = SourceValue.Negate(operand);
                        return true;
                    }
                    catch (ChowException)
                    {
                        return false;
                    }
                case Operator.Not:
                    value = SourceValue.Not(operand);
                    return true;
                default:
                    return false;
            }
        }

        // fold_binop: arithmetic on two constants, within CPython's size guards. A failure (such as
        // division by zero) means it isn't folded, so the error happens at runtime as in CPython.
        static bool TryFoldBinary(ExpressionNode expression, out SourceValue value)
        {
            value = SourceValue.None;

            if (!IsFoldableOperator(expression.Operator)
                || !TryFold(expression.Left, out var left)
                || !TryFold(expression.Right, out var right)
                || !PassesSizeGuards(expression.Operator, left, right))
            {
                return false;
            }

            try
            {
                value = Evaluate(expression.Operator, left, right);
                return true;
            }
            catch (ChowException)
            {
                return false;
            }
        }

        static bool IsFoldableOperator(Operator op)
        {
            switch (op)
            {
                case Operator.Add:
                case Operator.Subtract:
                case Operator.Multiply:
                case Operator.Divide:
                case Operator.FloorDivide:
                case Operator.Modulus:
                case Operator.Exponentiate:
                case Operator.BinaryOr:
                case Operator.BinaryAnd:
                case Operator.BinaryXor:
                    return true;
                default:
                    // Comparisons, `and`/`or`, and the rest are never folded.
                    return false;
            }
        }

        // The evaluators take the right operand first, as the Processor pops them.
        static SourceValue Evaluate(Operator op, SourceValue left, SourceValue right)
        {
            switch (op)
            {
                case Operator.Add:
                    return SourceValue.Add(right, left);
                case Operator.Subtract:
                    return SourceValue.Subtract(right, left);
                case Operator.Multiply:
                    return SourceValue.Multiply(right, left);
                case Operator.Divide:
                    return SourceValue.Divide(right, left);
                case Operator.FloorDivide:
                    return SourceValue.Floor(right, left);
                case Operator.Modulus:
                    return SourceValue.Mod(right, left);
                case Operator.Exponentiate:
                    return SourceValue.Pow(right, left);
                case Operator.BinaryOr:
                    return SourceValue.BitwiseOr(right, left);
                case Operator.BinaryAnd:
                    return SourceValue.BitwiseAnd(right, left);
                case Operator.BinaryXor:
                    return SourceValue.BitwiseXor(right, left);
                default:
                    throw new UnreachableException(nameof(Evaluate));
            }
        }

        // safe_multiply, safe_power, and safe_mod
        static bool PassesSizeGuards(Operator op, SourceValue left, SourceValue right)
        {
            switch (op)
            {
                case Operator.Multiply:
                    if (IsInt(left) && IsInt(right))
                    {
                        return left.ToLong() == 0 || right.ToLong() == 0
                            || BitLength(left.ToLong()) + BitLength(right.ToLong()) <= MaxIntBits;
                    }

                    if (IsInt(left) && right.DataType == DataType.Str)
                    {
                        return IsRepeatAllowed(left.ToLong(), right.ToString());
                    }

                    if (IsInt(right) && left.DataType == DataType.Str)
                    {
                        return IsRepeatAllowed(right.ToLong(), left.ToString());
                    }

                    return true;
                case Operator.Exponentiate:
                    if (IsInt(left) && IsInt(right) && left.ToLong() != 0 && right.ToLong() > 0)
                    {
                        return BitLength(left.ToLong()) <= MaxIntBits / right.ToLong();
                    }

                    return true;
                case Operator.Modulus:
                    // `%` on a string is formatting, which CPython never folds here.
                    return left.DataType != DataType.Str;
                default:
                    return true;
            }
        }

        static bool IsRepeatAllowed(long count, string text)
        {
            var length = CodePointCount(text);
            return length == 0 || count >= 0 && count <= MaxStrLength / length;
        }

        // bool is a subclass of int in Python, so it counts as one here.
        static bool IsInt(SourceValue value)
        {
            return value.DataType == DataType.Long || value.DataType == DataType.Bool;
        }

        // _PyLong_NumBits: the bit length of |value|.
        static int BitLength(long value)
        {
            var magnitude = value < 0 ? (ulong)(-(value + 1)) + 1 : (ulong)value;
            var bits = 0;

            while (magnitude != 0)
            {
                bits++;
                magnitude >>= 1;
            }

            return bits;
        }

        // CPython measures string length in code points, not UTF-16 units.
        static int CodePointCount(string text)
        {
            var count = 0;

            for (var i = 0; i < text.Length; i++)
            {
                if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    i++;
                }

                count++;
            }

            return count;
        }
    }
}
