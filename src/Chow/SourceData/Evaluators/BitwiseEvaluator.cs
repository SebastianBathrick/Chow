using Chow.Code;
using Chow.Interpreter.Exceptions;
using Chow.Interpreter.VM;

namespace Chow.SourceData
{
    /// <summary>
    /// Static service class for <see cref="Processor"/> that evaluates <c>|</c>, <c>&amp;</c>, and
    /// <c>^</c> as Python does: bitwise on ints and bools, set algebra on sets and dict keys views, and
    /// for <c>|</c>, dict merge.
    /// </summary>
    static class BitwiseEvaluator
    {
        public static SourceValue Or(ref SourceValue r, ref SourceValue l)
        {
            if (AreIntegers(ref l, ref r))
            {
                return IntegerResult(ref l, ref r, l.ToLong() | r.ToLong());
            }

            if (l.DataType == DataType.Dict && r.DataType == DataType.Dict)
            {
                // PEP 584 merge
                return new SourceValue(SourceDict.Merge((SourceDict)l.ToObject(), (SourceDict)r.ToObject()));
            }

            if (SourceSet.IsAnySet(l) && SourceSet.IsAnySet(r))
            {
                return new SourceValue(((SourceSet)l.ToObject()).Or((SourceSet)r.ToObject()));
            }

            if (IsEitherKeysView(ref l, ref r))
            {
                return new SourceValue(SourceDictKeys.Or(l, r));
            }

            throw UnsupportedBinary(ref l, ref r, Operator.BinaryOr);
        }

        public static SourceValue And(ref SourceValue r, ref SourceValue l)
        {
            if (AreIntegers(ref l, ref r))
            {
                return IntegerResult(ref l, ref r, l.ToLong() & r.ToLong());
            }

            if (SourceSet.IsAnySet(l) && SourceSet.IsAnySet(r))
            {
                return new SourceValue(((SourceSet)l.ToObject()).Intersection(r));
            }

            if (IsEitherKeysView(ref l, ref r))
            {
                return new SourceValue(SourceDictKeys.And(l, r));
            }

            throw UnsupportedBinary(ref l, ref r, Operator.BinaryAnd);
        }

        public static SourceValue Xor(ref SourceValue r, ref SourceValue l)
        {
            if (AreIntegers(ref l, ref r))
            {
                return IntegerResult(ref l, ref r, l.ToLong() ^ r.ToLong());
            }

            if (SourceSet.IsAnySet(l) && SourceSet.IsAnySet(r))
            {
                return new SourceValue(((SourceSet)l.ToObject()).SymmetricDifference(r));
            }

            if (IsEitherKeysView(ref l, ref r))
            {
                return new SourceValue(SourceDictKeys.Xor(l, r));
            }

            throw UnsupportedBinary(ref l, ref r, Operator.BinaryXor);
        }

        // bool is a subclass of int in Python, so ints and bools mix freely.
        static bool AreIntegers(ref SourceValue l, ref SourceValue r)
        {
            return (l.DataType == DataType.Long || l.DataType == DataType.Bool)
                && (r.DataType == DataType.Long || r.DataType == DataType.Bool);
        }

        // bool's own & | ^ return a bool only when both operands are bools: True & 1 is 1.
        static SourceValue IntegerResult(ref SourceValue l, ref SourceValue r, long result)
        {
            return l.DataType == DataType.Bool && r.DataType == DataType.Bool
                ? new SourceValue(result != 0)
                : new SourceValue(result);
        }

        // A keys view takes any iterable on either side, so the right operand's view handles
        // `[1, 2] - d.keys()` once the left operand declines.
        static bool IsEitherKeysView(ref SourceValue l, ref SourceValue r)
        {
            return l.DataType == DataType.DictKeys || r.DataType == DataType.DictKeys;
        }

        static DataTypeException UnsupportedBinary(ref SourceValue l, ref SourceValue r, Operator op)
        {
            // Message shape mirrors Python's TypeError wording and type names.
            return new DataTypeException(
                $"unsupported operand type(s) for {OperatorStrings.ToSource(op)}: "
                + $"'{DataTypeNames.GetTypeName(l)}' and '{DataTypeNames.GetTypeName(r)}'");
        }
    }
}
