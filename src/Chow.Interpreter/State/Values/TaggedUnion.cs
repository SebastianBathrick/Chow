using System;

namespace Chow.Interpreter.State.Values
{
    struct TaggedUnion
    {
        #region Constants
        
        const double DEFAULT_FLOAT_VALUE = 0.0;
        const long DEFAULT_INT_VALUE = 0L;
        const bool DEFAULT_BOOL_VALUE = false;
        const object DEFAULT_NULL_VALUE = null;
        
        #endregion
        
        #region Fields
        
        public static TaggedUnion Empty = new TaggedUnion(Tag.Empty);
        public static TaggedUnion None = new TaggedUnion(Tag.None);

        // TODO: Test whether using explicit struct layouts meaningly affects performance
        // If it seems it reduces execution time then Tag likely will become a byte enum
        Tag _tag;
        bool _bool;
        object _obj;
        long _longInt;
        double _doubleFloat;

        #endregion
        
        #region Properties
        
        // TODO: Get rid of these, because .Tag does the same thing with not that much more code
        public Tag Tag => _tag;

        public bool IsEmpty => Tag == Tag.Empty;
        
        public bool IsInt => Tag == Tag.Int;
        
        public bool IsFloat => Tag == Tag.Float;
        
        public bool IsString => Tag == Tag.Str;
        
        public bool IsBoolean => Tag == Tag.Boolean;
        
        public bool IsObject => Tag == Tag.Object;
        
        public bool IsList => Tag == Tag.List;

        public bool IsDict => Tag == Tag.Dict;

        public bool IsRange => Tag == Tag.Range;
        
                public bool IsTruthy
        {
            get
            {
                switch (Tag)
                {
                    case Tag.Boolean:
                        return _bool;
                    case Tag.Int:
                        return _longInt != 0;
                    case Tag.Float:
                        return _doubleFloat != 0.0;
                    case Tag.Str:
                        return ((string)_obj).Length > 0;
                    case Tag.List:
                        return ((InternalList)_obj).Count > 0;
                    case Tag.Dict:
                        return ((InternalDict)_obj).Count > 0;
                    case Tag.Range:
                        return ((InternalRange)_obj).Count > 0;
                    case Tag.Object:
                        return _obj != null;
                    default:
                        return false;
                }
            }
        }

        public long IntegerValue
        {
            get
            {
                ValidateTaggedUnionType(Tag.Int);
                return _longInt;
            }
            set
            {
                ValidateTaggedUnionType(Tag.Int);
                _longInt = value;
            }
        }

        public double FloatValue
        {
            get
            {
                ValidateTaggedUnionType(Tag.Float);
                return _doubleFloat;
            }
            set
            {
                ValidateTaggedUnionType(Tag.Float);
                _doubleFloat = value;
            }
        }

        public string StringValue
        {
            get
            {
                if (!IsString)
                {
                    throw new InvalidOperationException($"String access attempt but union's type is {Tag}");
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
                    throw new InvalidOperationException($"List access attempt but union's type is {Tag}");
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
                    throw new InvalidOperationException($"Dict access attempt but union's type is {Tag}");
                }
                return (InternalDict)_obj;
            }
        }

        public InternalRange RangeValue
        {
            get
            {
                if (!IsRange)
                {
                    throw new InvalidOperationException($"Range access attempt but union's type is {Tag}");
                }
                return (InternalRange)_obj;
            }
        }

        
        #endregion
        
        #region Constructors
        
        TaggedUnion(Tag type)
        {
            _tag = type;
            _longInt = DEFAULT_INT_VALUE;
            _doubleFloat = DEFAULT_FLOAT_VALUE;
            _bool = DEFAULT_BOOL_VALUE;
            _obj = DEFAULT_NULL_VALUE;
        }

        public TaggedUnion(double value) : this(Tag.Float)
        {
            _doubleFloat = value;
        }

        public TaggedUnion(long value) : this(Tag.Int)
        {
            _longInt = value;
        }

        public TaggedUnion(string value) : this(Tag.Str)
        {
            _obj = value;
        }

        public TaggedUnion(bool value) : this(Tag.Boolean)
        {
            _bool = value;
        }

        public TaggedUnion(object value) : this(Tag.Object)
        {
            _obj = value;
        }

        public TaggedUnion(InternalList list) : this(Tag.List)
        {
            _obj = list;
        }

        public TaggedUnion(InternalDict dict) : this(Tag.Dict)
        {
            _obj = dict;
        }

        public TaggedUnion(InternalRange range) : this(Tag.Range)
        {
            _obj = range;
        }

        #endregion



        public static TaggedUnion CreateWithValue(object value)
        {
            if (value == null)
            {
                return None;
            }

            switch (value)
            {
                case int i:
                    return new TaggedUnion((long)i);
                case long l:
                    return new TaggedUnion(l);
                case float f:
                    return new TaggedUnion((double)f);
                case double d:
                    return new TaggedUnion(d);
                case bool b:
                    return new TaggedUnion(b);
                case string s:
                    return new TaggedUnion(s);
                case InternalList list:
                    return new TaggedUnion(list);
                case InternalDict dict:
                    return new TaggedUnion(dict);
                case InternalRange range:
                    return new TaggedUnion(range);
                default:
                    return new TaggedUnion(value);
            }
        }

        public object GetTaggedValue()
        {
            switch (Tag)
            {
                case Tag.Int:
                    return _longInt;
                case Tag.Float:
                    return _doubleFloat;
                case Tag.Boolean:
                    return _bool;
                case Tag.Str:
                case Tag.Object:
                case Tag.List:
                case Tag.Dict:
                case Tag.Range:
                    return _obj;
                case Tag.None:
                    return null;
                default:
                    throw new InvalidOperationException($"Cannot get value of TaggedUnion with type {Tag}");
            }
        }

        public TaggedUnion MakeInteropCall(TaggedUnion[] args)
        {
            if (!IsObject)
            {
                throw new InvalidOperationException($"'{Tag}' object is not callable");
            }

            if (_obj is Func<TaggedUnion[], TaggedUnion> methodDelegate)
            {
                return methodDelegate(args ?? Array.Empty<TaggedUnion>());
            }

            throw new InvalidOperationException($"Object of type '{_obj.GetType().Name}' is not callable");
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
                return new TaggedUnion(InternalList.Repeat((InternalList)left._obj, (int)right.IntegerValue));
            }
            if (left.IsInt && right.IsList)
            {
                return new TaggedUnion(InternalList.Repeat((InternalList)right._obj, (int)left.IntegerValue));
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
                var a = BoolAsInt(left);
                var b = BoolAsInt(right);
                return new TaggedUnion((a % b + b) % b);
            }
            if (EitherIsFloat(left, right))
            {
                var l = AsFloat(left);
                var r = AsFloat(right);
                return new TaggedUnion((l % r + r) % r);
            }

            var ai = left.IntegerValue;
            var bi = right.IntegerValue;
            return new TaggedUnion((ai % bi + bi) % bi);
        }

        public static TaggedUnion FloorDivide(TaggedUnion left, TaggedUnion right)
        {
            ThrowIfObjectOperands(left, right);
            // TODO: Remove once bool<->numeric coercion is implemented (Python coerces bool to int in mixed arithmetic).
            ThrowIfMixedBoolNumeric(left, right);
            // Python semantics: floors toward negative infinity.
            if (BothAreBoolean(left, right))
            {
                return new TaggedUnion((long)Math.Floor(BoolAsInt(left) / (double)BoolAsInt(right)));
            }
            if (EitherIsFloat(left, right))
            {
                return new TaggedUnion(Math.Floor(AsFloat(left) / AsFloat(right)));
            }

            return new TaggedUnion((long)Math.Floor(left.IntegerValue / (double)right.IntegerValue));
        }

        public static TaggedUnion Power(TaggedUnion left, TaggedUnion right)
        {
            ThrowIfObjectOperands(left, right);
            // TODO: Remove once bool<->numeric coercion is implemented (Python coerces bool to int in mixed arithmetic).
            ThrowIfMixedBoolNumeric(left, right);
            // Python semantics: float if either operand is float, or if exponent is negative.
            if (BothAreBoolean(left, right))
            {
                return new TaggedUnion((long)Math.Pow(BoolAsInt(left), BoolAsInt(right)));
            }
            if (EitherIsFloat(left, right))
            {
                return new TaggedUnion(Math.Pow(AsFloat(left), AsFloat(right)));
            }

            var exp = right.IntegerValue;
            if (exp < 0)
            {
                return new TaggedUnion(Math.Pow(left.IntegerValue, exp));
            }

            return new TaggedUnion((long)Math.Pow(left.IntegerValue, exp));
        }

        public static TaggedUnion operator |(TaggedUnion left, TaggedUnion right)
        {
            if (left.IsDict && right.IsDict)
            {
                return new TaggedUnion(InternalDict.Merge((InternalDict)left._obj, (InternalDict)right._obj));
            }
            throw new InvalidOperationException($"unsupported operand type(s) for |: '{left.Tag}' and '{right.Tag}'");
        }

        public static bool operator ==(TaggedUnion left, TaggedUnion right)
        {
            if (left.Tag != right.Tag)
            {
                return false;
            }

            switch (left.Tag)
            {
                case Tag.Int:
                    return left._longInt == right._longInt;
                case Tag.Float:
                    return left._doubleFloat == right._doubleFloat;
                case Tag.Boolean:
                    return left._bool == right._bool;
                case Tag.Str:
                    return (string)left._obj == (string)right._obj;
                case Tag.List:
                    return InternalList.ElementsEqual((InternalList)left._obj, (InternalList)right._obj);
                case Tag.Dict:
                    return InternalDict.ElementsEqual((InternalDict)left._obj, (InternalDict)right._obj);
                case Tag.Range:
                    return ReferenceEquals(left._obj, right._obj);
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

        static long BoolAsInt(TaggedUnion union)
        {
            return union.BooleanValue ? 1L : 0L;
        }

        static void ThrowIfObjectOperands(TaggedUnion left, TaggedUnion right)
        {
            if (left.IsObject || right.IsObject || left.IsString || right.IsString || left.IsList || right.IsList || left.IsDict || right.IsDict || left.IsRange || right.IsRange)
            {
                throw new InvalidOperationException("Object operands are not supported for this operation.");
            }
        }

        // TODO: Temporary guard. Delete when mixed bool/numeric coercion is implemented.
        static void ThrowIfMixedBoolNumeric(TaggedUnion left, TaggedUnion right)
        {
            var leftBool = left.IsBoolean;
            var rightBool = right.IsBoolean;
            if (leftBool == rightBool)
            {
                return;
            }

            var otherIsNumeric = leftBool ? right.IsInt || right.IsFloat : left.IsInt || left.IsFloat;
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
            switch (Tag)
            {
                case Tag.Int:
                    return _longInt.GetHashCode();
                case Tag.Float:
                    return _doubleFloat.GetHashCode();
                case Tag.Boolean:
                    return _bool.GetHashCode();
                case Tag.Str:
                case Tag.Object:
                case Tag.Range:
                    return _obj?.GetHashCode() ?? 0;
                default:
                    return Tag.GetHashCode();
            }
        }

        public override string ToString()
        {
            if (IsEmpty)
            {
                return $"TaggedUnion(type={Tag})";
            }

            if (IsFloat)
            {
                return $"TaggedUnion(type={Tag}, value={_doubleFloat})";
            }

            if (IsBoolean)
            {
                return $"TaggedUnion(type={Tag}, value={_bool})";
            }

            if (IsObject || IsString || IsList || IsDict || IsRange)
            {
                return $"TaggedUnion(type={Tag}, value={_obj})";
            }

            return $"TaggedUnion(type={Tag}, value={_longInt})";
        }

        void ValidateTaggedUnionType(Tag desiredType)
        {
            if (Tag == desiredType)
            {
                return;
            }

            // TODO: Replace this branch with actual coercion (bool<->int<->float) once implemented.
            if (IsCoercibleNumeric(desiredType) && IsCoercibleNumeric(Tag))
            {
                throw new NotImplementedException($"{desiredType} access on union of type {Tag} requires coercion that is not yet implemented.");
            }

            throw new InvalidOperationException($"{desiredType} access attempt but union's type is {Tag}");
        }

        // TODO: Temporary helper used only by the coercion-not-implemented guard. Remove with that guard.
        static bool IsCoercibleNumeric(Tag type)
        {
            return type == Tag.Boolean || type == Tag.Int || type == Tag.Float;
        }
    }
}
