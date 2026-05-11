using System;

namespace Chow.Interpreter.Values.Internal
{
    struct TaggedUnion
    {
        const double DEFAULT_FLOAT_VALUE = 0.0;
        const int DEFAULT_INT_VALUE = 0;
        const bool DEFAULT_BOOL_VALUE = false;
        const object DEFAULT_NULL_VALUE = null;

        // TODO: Test whether using explicit struct layouts meaningly affects performance
        Tag _type;
        int _int;
        double _float;
        bool _bool;
        object _obj;

        public static TaggedUnion Empty = new TaggedUnion(Tag.Empty);
        public static TaggedUnion None = new TaggedUnion(Tag.None);

        public Tag Tag => _type;

        public bool IsEmpty => _type == Tag.Empty;
        public bool IsInt => _type == Tag.Int;
        public bool IsFloat => _type == Tag.Float;
        public bool IsString => _type == Tag.Str;
        public bool IsBoolean => _type == Tag.Boolean;
        public bool IsObject => _type == Tag.Object;
        public bool IsList => _type == Tag.List;
        public bool IsDict => _type == Tag.Dict;

        public bool IsTruthy
        {
            get
            {
                switch (_type)
                {
                    case Tag.Boolean:
                        return _bool;
                    case Tag.Int:
                        return _int != 0;
                    case Tag.Float:
                        return _float != 0.0;
                    case Tag.Str:
                        return ((string)_obj).Length > 0;
                    case Tag.List:
                        return ((InternalList)_obj).Count > 0;
                    case Tag.Dict:
                        return ((InternalDict)_obj).Count > 0;
                    default:
                        return false;
                }
            }
        }

        public int IntegerValue
        {
            get
            {
                ValidateTaggedUnionType(Tag.Int);
                return _int;
            }
            set
            {
                ValidateTaggedUnionType(Tag.Int);
                _int = value;
            }
        }

        public double FloatValue
        {
            get
            {
                ValidateTaggedUnionType(Tag.Float);
                return _float;
            }
            set
            {
                ValidateTaggedUnionType(Tag.Float);
                _float = value;
            }
        }

        public string StringValue
        {
            get
            {
                if (!IsString)
                {
                    throw new InvalidOperationException($"String access attempt but union's type is {_type}");
                }
                return (string)_obj;
            }
        }

        public bool BooleanValue
        {
            get
            {
                ValidateTaggedUnionType(Tag.Boolean);
                return _bool;
            }
            set
            {
                ValidateTaggedUnionType(Tag.Boolean);
                _bool = value;
            }
        }

        public object ObjectValue
        {
            get
            {
                ValidateTaggedUnionType(Tag.Object);
                return _obj;
            }
            set
            {
                ValidateTaggedUnionType(Tag.Object);
                _obj = value;
            }
        }

        public InternalList ListValue
        {
            get
            {
                if (!IsList)
                {
                    throw new InvalidOperationException($"List access attempt but union's type is {_type}");
                }
                return (InternalList)_obj;
            }
        }

        public InternalDict DictValue
        {
            get
            {
                if (!IsDict)
                {
                    throw new InvalidOperationException($"Dict access attempt but union's type is {_type}");
                }
                return (InternalDict)_obj;
            }
        }

        private TaggedUnion(Tag type)
        {
            _type = type;
            _int = DEFAULT_INT_VALUE;
            _float = DEFAULT_FLOAT_VALUE;
            _bool = DEFAULT_BOOL_VALUE;
            _obj = DEFAULT_NULL_VALUE;
        }

        public TaggedUnion(double value)
        {
            _float = value;
            _type = Tag.Float;
            _bool = DEFAULT_BOOL_VALUE;
            _obj = DEFAULT_NULL_VALUE;
            _int = DEFAULT_INT_VALUE;
        }

        public TaggedUnion(int value)
        {
            _int = value;
            _type = Tag.Int;
            _float = DEFAULT_FLOAT_VALUE;
            _bool = DEFAULT_BOOL_VALUE;
            _obj = DEFAULT_NULL_VALUE;
        }

        public TaggedUnion(string value)
        {
            _obj = value;
            _type = Tag.Str;
            _int = DEFAULT_INT_VALUE;
            _float = DEFAULT_FLOAT_VALUE;
            _bool = DEFAULT_BOOL_VALUE;
        }

        public TaggedUnion(bool value)
        {
            _bool = value;
            _type = Tag.Boolean;
            _int = DEFAULT_INT_VALUE;
            _float = DEFAULT_FLOAT_VALUE;
            _obj = DEFAULT_NULL_VALUE;
        }

        public TaggedUnion(object value)
        {
            _obj = value;
            _type = Tag.Object;
            _int = DEFAULT_INT_VALUE;
            _float = DEFAULT_FLOAT_VALUE;
            _bool = DEFAULT_BOOL_VALUE;
        }

        public TaggedUnion(InternalList list)
        {
            _obj = list;
            _type = Tag.List;
            _int = DEFAULT_INT_VALUE;
            _float = DEFAULT_FLOAT_VALUE;
            _bool = DEFAULT_BOOL_VALUE;
        }

        public TaggedUnion(InternalDict dict)
        {
            _obj = dict;
            _type = Tag.Dict;
            _int = DEFAULT_INT_VALUE;
            _float = DEFAULT_FLOAT_VALUE;
            _bool = DEFAULT_BOOL_VALUE;
        }

        /// <summary>
        ///  Makes call to a value that is not a function declared in Chow source code, but instead a client-provided 
        ///  delegate types that optionally accept <see cref="ChowValue"/> parameters and can return a <see cref="ChowValue"/>.
        /// </summary>
        /// <param name="singleArg">The first argument used for the call or null.</param>
        /// <param name="args">An array of additional arguments for the call or null.</param>
        /// <returns>The TaggedUnion result of the call. If the interop function returns void, the result is a None value.</returns>
        /// <exception cref="InvalidOperationException">If the object is not callable.</exception>
        /// <remarks><paramref name="singleArg"/> is its own parameter so that a new array does not have to be created for a single element.</remarks>
        public TaggedUnion MakeInteropCall(TaggedUnion? singleArg, TaggedUnion[] args)
        {
            if (!IsObject)
            {
                // TODO: Replace with TypeErrorException once implemented
                throw new InvalidOperationException($"'{_type}' object is not callable");
            }

            switch (_obj)
            {
                // FUTURE: this delegate case also serves class-bound methods (closure pre-binding `self`).
                case Func<TaggedUnion[], TaggedUnion> methodDelegate:
                    TaggedUnion[] allArgs;
                    if (singleArg.HasValue)
                    {
                        allArgs = new[] { singleArg.Value };
                    }
                    else
                    {
                        allArgs = args ?? Array.Empty<TaggedUnion>();
                    }
                    return methodDelegate(allArgs);

                case Func<ChowValue> funcNoArg:
                    return ApiValueConverter.ToTaggedUnion(funcNoArg());
                case Func<ChowValue, ChowValue> funcOneArg:
                    return ApiValueConverter.ToTaggedUnion(funcOneArg(ApiValueConverter.ToApiClassObj(singleArg.Value)));
                case Func<ChowValue[], ChowValue> funcManyArgs:
                    return ApiValueConverter.ToTaggedUnion(funcManyArgs(BuildArgArray(args)));
                case Action action:
                    action();
                    return TaggedUnion.None;
                case Action<ChowValue> actionOneArg:
                    actionOneArg(ApiValueConverter.ToApiClassObj(singleArg.Value));
                    return TaggedUnion.None;
                case Action<ChowValue[]> actionManyArgs:
                    actionManyArgs(BuildArgArray(args));
                    return TaggedUnion.None;
                default:
                    throw new InvalidOperationException($"Object of type '{_obj.GetType().Name}' is not callable");
            }
        }

        static ChowValue[] BuildArgArray(TaggedUnion[] args)
        {
            ChowValue[] result = new ChowValue[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                result[i] = ApiValueConverter.ToApiClassObj(args[i]);
            }
            return result;
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
            // FUTURE: list-specific carve-out. Future container types (e.g. tuples, future immutable seqs) add their own carve-outs.
            if (left.IsList && right.IsList)
            {
                return new TaggedUnion(InternalList.Concat((InternalList)left._obj, (InternalList)right._obj));
            }
            ThrowIfObjectOperands(left, right);
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
            ThrowIfObjectOperands(left, right);
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
            // FUTURE: list-specific carve-out. Future container types add their own carve-outs.
            if (left.IsList && right.IsInt)
            {
                return new TaggedUnion(InternalList.Repeat((InternalList)left._obj, right.IntegerValue));
            }
            if (left.IsInt && right.IsList)
            {
                return new TaggedUnion(InternalList.Repeat((InternalList)right._obj, left.IntegerValue));
            }
            ThrowIfObjectOperands(left, right);
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
            ThrowIfObjectOperands(left, right);
            // TODO: Remove once bool<->numeric coercion is implemented (Python coerces bool to int in mixed arithmetic).
            ThrowIfMixedBoolNumeric(left, right);
            // Python semantics: `/` always produces a float, even for int / int.
            if (BothAreBoolean(left, right))
            {
                return new TaggedUnion((double)BoolAsInt(left) / BoolAsInt(right));
            }
            return new TaggedUnion(AsFloat(left) / AsFloat(right));
        }

        public static TaggedUnion operator %(TaggedUnion left, TaggedUnion right)
        {
            ThrowIfObjectOperands(left, right);
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
                double l = AsFloat(left);
                double r = AsFloat(right);
                return new TaggedUnion(((l % r) + r) % r);
            }

            int ai = left.IntegerValue;
            int bi = right.IntegerValue;
            return new TaggedUnion(((ai % bi) + bi) % bi);
        }

        public static TaggedUnion FloorDivide(TaggedUnion left, TaggedUnion right)
        {
            ThrowIfObjectOperands(left, right);
            // TODO: Remove once bool<->numeric coercion is implemented (Python coerces bool to int in mixed arithmetic).
            ThrowIfMixedBoolNumeric(left, right);
            // Python semantics: floors toward negative infinity.
            if (BothAreBoolean(left, right))
            {
                return new TaggedUnion((int)Math.Floor(BoolAsInt(left) / (double)BoolAsInt(right)));
            }
            if (EitherIsFloat(left, right))
            {
                return new TaggedUnion(Math.Floor(AsFloat(left) / AsFloat(right)));
            }

            return new TaggedUnion((int)Math.Floor(left.IntegerValue / (double)right.IntegerValue));
        }

        public static TaggedUnion Power(TaggedUnion left, TaggedUnion right)
        {
            ThrowIfObjectOperands(left, right);
            // TODO: Remove once bool<->numeric coercion is implemented (Python coerces bool to int in mixed arithmetic).
            ThrowIfMixedBoolNumeric(left, right);
            // Python semantics: float if either operand is float, or if exponent is negative.
            if (BothAreBoolean(left, right))
            {
                return new TaggedUnion((int)Math.Pow(BoolAsInt(left), BoolAsInt(right)));
            }
            if (EitherIsFloat(left, right))
            {
                return new TaggedUnion(Math.Pow(AsFloat(left), AsFloat(right)));
            }

            int exp = right.IntegerValue;
            if (exp < 0)
            {
                return new TaggedUnion(Math.Pow(left.IntegerValue, exp));
            }

            return new TaggedUnion((int)Math.Pow(left.IntegerValue, exp));
        }

        public static TaggedUnion operator |(TaggedUnion left, TaggedUnion right)
        {
            if (left.IsDict && right.IsDict)
            {
                return new TaggedUnion(InternalDict.Merge((InternalDict)left._obj, (InternalDict)right._obj));
            }
            throw new InvalidOperationException($"unsupported operand type(s) for |: '{left._type}' and '{right._type}'");
        }

        public static bool operator ==(TaggedUnion left, TaggedUnion right)
        {
            if (left._type != right._type)
            {
                return false;
            }

            switch (left._type)
            {
                case Tag.Int:
                    return left._int == right._int;
                case Tag.Float:
                    return left._float == right._float;
                case Tag.Boolean:
                    return left._bool == right._bool;
                case Tag.Str:
                    return (string)left._obj == (string)right._obj;
                case Tag.List:
                    return InternalList.ElementsEqual((InternalList)left._obj, (InternalList)right._obj);
                case Tag.Dict:
                    return InternalDict.ElementsEqual((InternalDict)left._obj, (InternalDict)right._obj);
                case Tag.Object:
                    return ReferenceEquals(left._obj, right._obj);
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
            ThrowIfObjectOperands(left, right);
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
            ThrowIfObjectOperands(left, right);
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
            ThrowIfObjectOperands(left, right);
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
            ThrowIfObjectOperands(left, right);
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

        static void ThrowIfObjectOperands(TaggedUnion left, TaggedUnion right)
        {
            if (left.IsObject || right.IsObject || left.IsString || right.IsString || left.IsList || right.IsList || left.IsDict || right.IsDict)
            {
                throw new InvalidOperationException("Object operands are not supported for this operation.");
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

            bool otherIsNumeric = leftBool ? (right.IsInt || right.IsFloat) : (left.IsInt || left.IsFloat);
            if (otherIsNumeric)
            {
                throw new NotImplementedException("Mixed boolean and numeric operands are not yet implemented.");
            }
        }

        static double AsFloat(TaggedUnion union)
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
                case Tag.Int:
                    return _int.GetHashCode();
                case Tag.Float:
                    return _float.GetHashCode();
                case Tag.Boolean:
                    return _bool.GetHashCode();
                case Tag.Str:
                case Tag.Object:
                    return _obj?.GetHashCode() ?? 0;
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
                return $"TaggedUnion(type={_type}, value={_float})";
            }

            if (IsBoolean)
            {
                return $"TaggedUnion(type={_type}, value={_bool})";
            }

            if (IsObject || IsString)
            {
                return $"TaggedUnion(type={_type}, value={_obj})";
            }

            return $"TaggedUnion(type={_type}, value={_int})";
        }

        void ValidateTaggedUnionType(Tag desiredType)
        {
            if (_type == desiredType)
            {
                return;
            }

            // TODO: Replace this branch with actual coercion (bool<->int<->float) once implemented.
            if (IsCoercibleNumeric(desiredType) && IsCoercibleNumeric(_type))
            {
                throw new NotImplementedException($"{desiredType} access on union of type {_type} requires coercion that is not yet implemented.");
            }

            throw new InvalidOperationException($"{desiredType} access attempt but union's type is {_type}");
        }

        // TODO: Temporary helper used only by the coercion-not-implemented guard. Remove with that guard.
        static bool IsCoercibleNumeric(Tag type)
        {
            return type == Tag.Boolean || type == Tag.Int || type == Tag.Float;
        }
    }
}
