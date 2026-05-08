using System;

namespace Chow.Interpreter.Values
{
    struct TaggedUnion
    {
        const float DEFAULT_FLOAT_VALUE = 0.0f;
        const int DEFAULT_INT_VALUE = 0;
        const string DEFAULT_STRING_VALUE = null;
        const bool DEFAULT_BOOL_VALUE = false;

        // TODO: Test whether using explicit struct layouts meaningly affects performance
        Tag _type;
        int _intValue;
        float _floatValue;
        bool _boolValue;

        // TODO: Add an object type and use it for strings instead of a separate field, to save space.
        string _stringValue;

        public static TaggedUnion Empty = new TaggedUnion(Tag.Empty);
        public static TaggedUnion None = new TaggedUnion(Tag.None);

        public Tag Tag => _type;

        public bool IsEmpty => _type == Tag.Empty;
        public bool IsNone => _type == Tag.None;
        public bool IsInteger => _type == Tag.Integer;
        public bool IsFloat => _type == Tag.Float;
        public bool IsString => _type == Tag.String;
        public bool IsBoolean => _type == Tag.Boolean;

        public bool IsTruthy
        {
            get
            {
                switch (_type)
                {
                    case Tag.Boolean:
                        return _boolValue;
                    case Tag.Integer:
                        return _intValue != 0;
                    case Tag.Float:
                        return _floatValue != 0f;
                    default:
                        return false;
                }
            }
        }

        public int IntegerValue
        {
            get
            {
                ValidateTaggedUnionType(Tag.Integer);
                return _intValue;
            }
            set
            {
                ValidateTaggedUnionType(Tag.Integer);
                _intValue = value;
            }
        }

        public float FloatValue
        {
            get
            {
                ValidateTaggedUnionType(Tag.Float);
                return _floatValue;
            }
            set
            {
                ValidateTaggedUnionType(Tag.Float);
                _floatValue = value;
            }
        }

        public string StringValue
        {
            get
            {
                ValidateTaggedUnionType(Tag.String);
                return _stringValue;
            }
            set
            {
                ValidateTaggedUnionType(Tag.String);
                _stringValue = value;
            }
        }

        public bool BooleanValue
        {
            get
            {
                ValidateTaggedUnionType(Tag.Boolean);
                return _boolValue;
            }
            set
            {
                ValidateTaggedUnionType(Tag.Boolean);
                _boolValue = value;
            }
        }

        private TaggedUnion(Tag type)
        {
            _type = type;
            _intValue = DEFAULT_INT_VALUE;
            _floatValue = DEFAULT_FLOAT_VALUE;
            _stringValue = DEFAULT_STRING_VALUE;
            _boolValue = DEFAULT_BOOL_VALUE;
        }

        public TaggedUnion(float value)
        {
            _floatValue = value;
            _type = Tag.Float;
            _intValue = DEFAULT_INT_VALUE;
            _stringValue = DEFAULT_STRING_VALUE;
            _boolValue = DEFAULT_BOOL_VALUE;
        }

        public TaggedUnion(int value)
        {
            _intValue = value;
            _type = Tag.Integer;
            _floatValue = DEFAULT_FLOAT_VALUE;
            _stringValue = DEFAULT_STRING_VALUE;
            _boolValue = DEFAULT_BOOL_VALUE;
        }

        public TaggedUnion(string value)
        {
            _stringValue = value;
            _type = Tag.String;
            _intValue = DEFAULT_INT_VALUE;
            _floatValue = DEFAULT_FLOAT_VALUE;
            _boolValue = DEFAULT_BOOL_VALUE;
        }

        public TaggedUnion(bool value)
        {
            _boolValue = value;
            _type = Tag.Boolean;
            _intValue = DEFAULT_INT_VALUE;
            _floatValue = DEFAULT_FLOAT_VALUE;
            _stringValue = DEFAULT_STRING_VALUE;
        }

        // TODO: Refactor operator overloads to create less new TaggedUnions by using helper functions that only use the
        // values for the type being mutated

        // TODO: Temporary reference table for current operator rules. Update as type support changes.
        // Operators covered: + - * / % FloorDivide Power
        //
        //   bool   op bool                -> int    ( /  -> float)
        //   int    op int                 -> int    ( /  -> float)
        //   float  op (int|float)         -> float
        //   int    op float               -> float
        //   int ** negative int           -> float
        //   bool   op (int|float)         -> NotImplementedException (coercion not implemented)
        //   any    op string              -> InvalidOperationException
        //   Empty/None as operand         -> InvalidOperationException (via property access)
        //   int|bool % 0, int|bool // 0   -> DivideByZeroException [low-priority: value-level]
        public static TaggedUnion operator +(TaggedUnion left, TaggedUnion right)
        {
            ThrowIfStringOperands(left, right);
            // TODO: Remove once bool<->numeric coercion is implemented (Python coerces bool to int in mixed arithmetic).
            ThrowIfMixedBoolNumeric(left, right);
            if (BothAreBoolean(left, right))
            {
                return new TaggedUnion(BoolAsInt(left) + BoolAsInt(right));
            }
            if (EitherIsFloat(left, right))
            {
                return new TaggedUnion(AsFloat(left) + AsFloat(right));
            }

            return new TaggedUnion(left.IntegerValue + right.IntegerValue);
        }

        public static TaggedUnion operator -(TaggedUnion left, TaggedUnion right)
        {
            ThrowIfStringOperands(left, right);
            // TODO: Remove once bool<->numeric coercion is implemented (Python coerces bool to int in mixed arithmetic).
            ThrowIfMixedBoolNumeric(left, right);
            if (BothAreBoolean(left, right))
            {
                return new TaggedUnion(BoolAsInt(left) - BoolAsInt(right));
            }
            if (EitherIsFloat(left, right))
            {
                return new TaggedUnion(AsFloat(left) - AsFloat(right));
            }

            return new TaggedUnion(left.IntegerValue - right.IntegerValue);
        }

        public static TaggedUnion operator *(TaggedUnion left, TaggedUnion right)
        {
            ThrowIfStringOperands(left, right);
            // TODO: Remove once bool<->numeric coercion is implemented (Python coerces bool to int in mixed arithmetic).
            ThrowIfMixedBoolNumeric(left, right);
            if (BothAreBoolean(left, right))
            {
                return new TaggedUnion(BoolAsInt(left) * BoolAsInt(right));
            }
            if (EitherIsFloat(left, right))
            {
                return new TaggedUnion(AsFloat(left) * AsFloat(right));
            }

            return new TaggedUnion(left.IntegerValue * right.IntegerValue);
        }

        public static TaggedUnion operator /(TaggedUnion left, TaggedUnion right)
        {
            ThrowIfStringOperands(left, right);
            // TODO: Remove once bool<->numeric coercion is implemented (Python coerces bool to int in mixed arithmetic).
            ThrowIfMixedBoolNumeric(left, right);
            // Python semantics: `/` always produces a float, even for int / int.
            if (BothAreBoolean(left, right))
            {
                return new TaggedUnion((float)BoolAsInt(left) / BoolAsInt(right));
            }
            return new TaggedUnion(AsFloat(left) / AsFloat(right));
        }

        public static TaggedUnion operator %(TaggedUnion left, TaggedUnion right)
        {
            ThrowIfStringOperands(left, right);
            // TODO: Remove once bool<->numeric coercion is implemented (Python coerces bool to int in mixed arithmetic).
            ThrowIfMixedBoolNumeric(left, right);
            // Python semantics: result has the sign of the divisor.
            if (BothAreBoolean(left, right))
            {
                int a = BoolAsInt(left);
                int b = BoolAsInt(right);
                return new TaggedUnion(((a % b) + b) % b);
            }
            if (EitherIsFloat(left, right))
            {
                float l = AsFloat(left);
                float r = AsFloat(right);
                return new TaggedUnion(((l % r) + r) % r);
            }

            int ai = left.IntegerValue;
            int bi = right.IntegerValue;
            return new TaggedUnion(((ai % bi) + bi) % bi);
        }

        public static TaggedUnion FloorDivide(TaggedUnion left, TaggedUnion right)
        {
            ThrowIfStringOperands(left, right);
            // TODO: Remove once bool<->numeric coercion is implemented (Python coerces bool to int in mixed arithmetic).
            ThrowIfMixedBoolNumeric(left, right);
            // Python semantics: floors toward negative infinity.
            if (BothAreBoolean(left, right))
            {
                return new TaggedUnion((int)Math.Floor(BoolAsInt(left) / (double)BoolAsInt(right)));
            }
            if (EitherIsFloat(left, right))
            {
                return new TaggedUnion((float)Math.Floor(AsFloat(left) / (double)AsFloat(right)));
            }

            return new TaggedUnion((int)Math.Floor(left.IntegerValue / (double)right.IntegerValue));
        }

        public static TaggedUnion Power(TaggedUnion left, TaggedUnion right)
        {
            ThrowIfStringOperands(left, right);
            // TODO: Remove once bool<->numeric coercion is implemented (Python coerces bool to int in mixed arithmetic).
            ThrowIfMixedBoolNumeric(left, right);
            // Python semantics: float if either operand is float, or if exponent is negative.
            if (BothAreBoolean(left, right))
            {
                return new TaggedUnion((int)Math.Pow(BoolAsInt(left), BoolAsInt(right)));
            }
            if (EitherIsFloat(left, right))
            {
                return new TaggedUnion((float)Math.Pow(AsFloat(left), AsFloat(right)));
            }

            int exp = right.IntegerValue;
            if (exp < 0)
            {
                return new TaggedUnion((float)Math.Pow(left.IntegerValue, exp));
            }

            return new TaggedUnion((int)Math.Pow(left.IntegerValue, exp));
        }

        public static bool operator ==(TaggedUnion left, TaggedUnion right)
        {
            if (left._type != right._type)
            {
                return false;
            }

            switch (left._type)
            {
                case Tag.Integer:
                    return left._intValue == right._intValue;
                case Tag.Float:
                    return left._floatValue == right._floatValue;
                case Tag.String:
                    return left._stringValue == right._stringValue;
                case Tag.Boolean:
                    return left._boolValue == right._boolValue;
                default:
                    return true;
            }
        }

        public static bool operator !=(TaggedUnion left, TaggedUnion right)
        {
            return !(left == right);
        }

        public static bool operator <(TaggedUnion left, TaggedUnion right)
        {
            ThrowIfStringOperands(left, right);
            ThrowIfMixedBoolNumeric(left, right);
            if (BothAreBoolean(left, right))
            {
                return BoolAsInt(left) < BoolAsInt(right);
            }
            if (EitherIsFloat(left, right))
            {
                return AsFloat(left) < AsFloat(right);
            }

            return left.IntegerValue < right.IntegerValue;
        }

        public static bool operator >(TaggedUnion left, TaggedUnion right)
        {
            ThrowIfStringOperands(left, right);
            ThrowIfMixedBoolNumeric(left, right);
            if (BothAreBoolean(left, right))
            {
                return BoolAsInt(left) > BoolAsInt(right);
            }
            if (EitherIsFloat(left, right))
            {
                return AsFloat(left) > AsFloat(right);
            }

            return left.IntegerValue > right.IntegerValue;
        }

        public static bool operator <=(TaggedUnion left, TaggedUnion right)
        {
            ThrowIfStringOperands(left, right);
            ThrowIfMixedBoolNumeric(left, right);
            if (BothAreBoolean(left, right))
            {
                return BoolAsInt(left) <= BoolAsInt(right);
            }
            if (EitherIsFloat(left, right))
            {
                return AsFloat(left) <= AsFloat(right);
            }

            return left.IntegerValue <= right.IntegerValue;
        }

        public static bool operator >=(TaggedUnion left, TaggedUnion right)
        {
            ThrowIfStringOperands(left, right);
            ThrowIfMixedBoolNumeric(left, right);
            if (BothAreBoolean(left, right))
            {
                return BoolAsInt(left) >= BoolAsInt(right);
            }
            if (EitherIsFloat(left, right))
            {
                return AsFloat(left) >= AsFloat(right);
            }

            return left.IntegerValue >= right.IntegerValue;
        }

        static bool EitherIsFloat(TaggedUnion left, TaggedUnion right)
        {
            return left.IsFloat || right.IsFloat;
        }

        static bool BothAreBoolean(TaggedUnion left, TaggedUnion right)
        {
            return left.IsBoolean && right.IsBoolean;
        }

        static int BoolAsInt(TaggedUnion union)
        {
            return union.BooleanValue ? 1 : 0;
        }

        static void ThrowIfStringOperands(TaggedUnion left, TaggedUnion right)
        {
            if (left.IsString || right.IsString)
            {
                throw new InvalidOperationException("String operands are not supported for this operation.");
            }
        }

        // TODO: Temporary guard. Delete when mixed bool/numeric coercion is implemented.
        static void ThrowIfMixedBoolNumeric(TaggedUnion left, TaggedUnion right)
        {
            bool leftBool = left.IsBoolean;
            bool rightBool = right.IsBoolean;
            if (leftBool == rightBool)
            {
                return;
            }

            bool otherIsNumeric = leftBool ? (right.IsInteger || right.IsFloat) : (left.IsInteger || left.IsFloat);
            if (otherIsNumeric)
            {
                throw new NotImplementedException("Mixed boolean and numeric operands are not yet implemented.");
            }
        }

        static float AsFloat(TaggedUnion union)
        {
            if (union.IsFloat)
            {
                return union.FloatValue;
            }

            return union.IntegerValue;
        }

        public override bool Equals(object obj)
        {
            return obj is TaggedUnion other && this == other;
        }

        public override int GetHashCode()
        {
            switch (_type)
            {
                case Tag.Integer:
                    return _intValue.GetHashCode();
                case Tag.Float:
                    return _floatValue.GetHashCode();
                case Tag.String:
                    return _stringValue?.GetHashCode() ?? 0;
                case Tag.Boolean:
                    return _boolValue.GetHashCode();
                default:
                    return _type.GetHashCode();
            }
        }

        public override string ToString()
        {
            if (IsEmpty)
            {
                return $"TaggedUnion(type={_type})";
            }

            if (IsFloat)
            {
                return $"TaggedUnion(type={_type}, value={_floatValue})";
            }

            if (IsString)
            {
                return $"TaggedUnion(type={_type}, value={_stringValue})";
            }

            if (IsBoolean)
            {
                return $"TaggedUnion(type={_type}, value={_boolValue})";
            }

            return $"TaggedUnion(type={_type}, value={_intValue})";
        }

        void ValidateTaggedUnionType(Tag desiredType)
        {
            if (_type == desiredType)
            {
                return;
            }

            // TODO: Replace this branch with actual coercion (bool<->int<->float) once implemented.
            if (IsPythonCoercibleNumeric(desiredType) && IsPythonCoercibleNumeric(_type))
            {
                throw new NotImplementedException($"{desiredType} access on union of type {_type} requires Python coercion that is not yet implemented.");
            }

            throw new InvalidOperationException($"{desiredType} access attempt but union's type is {_type}");
        }

        // TODO: Temporary helper used only by the coercion-not-implemented guard. Remove with that guard.
        static bool IsPythonCoercibleNumeric(Tag type)
        {
            return type == Tag.Boolean
                || type == Tag.Integer
                || type == Tag.Float;
        }
    }
}
