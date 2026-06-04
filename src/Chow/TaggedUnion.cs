using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Chow.DataTypes;
using Chow.Exceptions;
namespace Chow
{
    /// <summary>
    /// Represents an immutable Chow value of varying Chow data types, with the main types being:
    /// <b>int, float, str, bool, None, list, dict, and range</b>.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct TaggedUnion
    {
        // TODO: Major refactor going on currently
        public static readonly TaggedUnion None = new TaggedUnion(Tag.None);
        static readonly Dictionary<Type, Tag> DataTypeMap = new Dictionary<Type, Tag>
        {
            { typeof(bool), Tag.Bool },
            { typeof(long), Tag.Long },
            { typeof(int), Tag.Long },
            { typeof(double), Tag.Double },
            { typeof(string), Tag.Str },
            { typeof(InternalDict), Tag.Dict },
            { typeof(InternalRange), Tag.Range },
            { typeof(InternalList), Tag.List }
        };

        #region Fields
        
        /// <summary>Represents the TaggedUnion equivalent to null/nil/none values.</summary>
        [FieldOffset(OBJ_FIELD_OFFSET)] readonly object _obj;
        [FieldOffset(LONG_FIELD_OFFSET)] readonly long _long;
        [FieldOffset(DBL_FIELD_OFFSET)] readonly double _dbl;
        [FieldOffset(TAG_FIELD_OFFSET)] readonly Tag _tag;

        #endregion

        #region Properties

        bool BoolValue => _long == BOOL_T_TO_LONG;

        internal Tag Tag => _tag;
        #endregion

        #region Constructors

        TaggedUnion(
            Tag tag = Tag.None,
            bool boolValue = NOT_BOOL_INIT,
            object objVal = NOT_OBJ_INIT,
            long longVal = NOT_LONG_INIT,
            double doubleVal = NOT_DBL_INIT)
        {
            _tag = tag;
            _obj = objVal;

            // _long and _dbl share the same FieldOffset (explicit-layout union). The compiler still
            // requires both to be definitely assigned, so every branch sets the live field to its
            // value and the dead field to its default — each field is written exactly once.
            switch (tag)
            {
                case Tag.Bool:
                    _dbl = NOT_DBL_INIT;
                    _long = boolValue ? BOOL_T_TO_LONG : BOOL_F_TO_LONG;
                    break;
                case Tag.Double:
                    _long = NOT_LONG_INIT;
                    _dbl = doubleVal;
                    break;
                case Tag.Object:
                case Tag.List:
                case Tag.Dict:
                case Tag.Range:
                case Tag.Str:
                case Tag.None:
                case Tag.Long:
                    _dbl = NOT_DBL_INIT;
                    _long = longVal;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(tag), tag, null);
            }
        }

        internal TaggedUnion(long value) : this(Tag.Long, longVal: value) {}

        internal TaggedUnion(double val) : this(Tag.Double, doubleVal: val) {}

        internal TaggedUnion(bool value) : this(Tag.Bool, value) {}

        internal TaggedUnion(string value) : this(Tag.Str, objVal: value) {}

        internal TaggedUnion(InternalList list) : this(Tag.List, objVal: list) {}

        internal TaggedUnion(InternalDict dict) : this(Tag.Dict, objVal: dict) {}

        internal TaggedUnion(InternalRange range) : this(Tag.Range, objVal: range) {}

        /// <summary>
        /// Resolves and converts the value of <paramref name="obj"/> and initializes instance with
        /// the converted value (if a tag is defined for that type).
        /// </summary>
        internal TaggedUnion(object obj)
        {
            switch (obj)
            {
                case null:
                // TODO: Look into changing this to TaggedUnion.None.
                    throw new ArgumentNullException(nameof(obj));
                case string strValue:
                    this = new TaggedUnion(strValue);
                    break;
                case long longValue:
                    this = new TaggedUnion(longValue);
                    break;
                case int intValue:
                    this = new TaggedUnion(intValue);
                    break;
                case double doubleValue:
                    this = new TaggedUnion(doubleValue);
                    break;
                case bool boolValue:
                    this = new TaggedUnion(boolValue);
                    break;
                case InternalList listValue:
                    this = new TaggedUnion(listValue);
                    break;
                case InternalDict dictValue:
                    this = new TaggedUnion(dictValue);
                    break;
                case InternalRange rangeValue:
                    this = new TaggedUnion(rangeValue);
                    break;
                case TaggedUnion chowValue:
                    // **IMPORTANT**: CHOW VALUES ARE NEVER DIRECTLY WRAPPED IN OTHER CHOW VALUE INSTANCES
                    this = chowValue;
                    break;
                default:
                    _tag = Tag.Object;
                    _obj = obj;
                    _long = NOT_LONG_INIT;
                    _dbl = NOT_DBL_INIT;
                    break;
            }
        }

        #endregion

        #region Type API

        /// <summary>Casts Chow value to specified host type, boxes, and returns it.</summary>
        /// <typeparam name="TDataType">The type the value will be casted to.</typeparam>
        /// <returns>The boxed and converted Chow value.</returns>
        /// <exception cref="InvalidOperationException">Throws an exception if the value stored 
        /// in this instance cannot be converted to the target type.</exception>
        public TDataType AsType<TDataType>()
        {
            var typeOf = typeof(TDataType);

            if (typeOf == typeof(object))
            {
                return (TDataType)ToObject();
            }

            if (!DataTypeMap.TryGetValue(typeOf, out var targetDataType))
            {
                if (_obj is TDataType typedObject)
                {
                    return typedObject;
                }

                throw new InvalidOperationException($"Cannot convert {_tag} to {typeOf}");
            }

            switch (targetDataType)
            {
                case Tag.Bool:
                {
                    return (TDataType)(object)ToBool();
                }
                case Tag.Long:
                {
                    // The map aliases both typeof(long) and typeof(int) to Tag.Long.
                    // For T == int we truncate; for T == long we return the full 64-bit value.
                    if (typeOf == typeof(int))
                    {
                        // TODO: Add error checking for overflow scenarios
                        return (TDataType)(object)(int)ToLong();
                    }

                    return (TDataType)(object)ToLong();
                }
                case Tag.Double:
                {
                    return (TDataType)(object)ToDouble();
                }
                case Tag.Str:
                {
                    return (TDataType)(object)ToStr();
                }
                case Tag.List:
                case Tag.Dict:
                case Tag.Range:
                {
                    if (_obj is TDataType typedObject)
                    {
                        return typedObject;
                    }

                    break;
                }
            }

            throw new InvalidOperationException($"Cannot convert {_tag} to {typeof(TDataType)}");
        }

        /// <summary>Whether the Chow value of this instance is of the provided data type.</summary>
        /// <typeparam name="TDataType">The data type to compare to the Chow value's data type.</typeparam>
        /// <returns>True if the Chow value of this instance is of type <typeparamref name=
        /// "TDataType"/>; otherwise, false.</returns>
        public bool IsOfType<TDataType>()
        {
            var checkType = typeof(TDataType);

            // If it is not a type defined by the Tag enum
            if (!DataTypeMap.TryGetValue(checkType, out var chowDataType))
            {
                return _tag == Tag.Object && _obj is TDataType;
            }

            // The map includes values representing data types that are from the Chow namespace
            return _tag == chowDataType;
        }

        public bool IsTruthy()
        {
            return ToBool();
        }

        #endregion

        #region Arithmetic & Logical Operations

        // Instance methods to avoid passing two ChowValues as parameters. Each returns a new TaggedUnion
        // (the struct is readonly, so no risk of accidentally mutating this instance's internal state).
        // Promotion rules come from DataTypeConversionMap (the single source of truth). Carve-outs for
        // container/string ops (list+list, list*int, str+str, str*int, dict|dict) are dispatched when
        // the map reports ConversionCase.Nothing.

        internal TaggedUnion CreateSum(TaggedUnion rightOperand)
        {
            switch (LookupBinary(ExpressionOperator.Add, rightOperand))
            {
                case ConversionCase.ToInt:
                {
                    return new TaggedUnion(PromoteToLong() + rightOperand.PromoteToLong());
                }
                case ConversionCase.ToFloat:
                {
                    return new TaggedUnion(PromoteToDouble() + rightOperand.PromoteToDouble());
                }
                case ConversionCase.Nothing:
                {
                    if (_tag == Tag.List && rightOperand._tag == Tag.List)
                    {
                        return new TaggedUnion(InternalList.Concat(AsType<InternalList>(), rightOperand.AsType<InternalList>()));
                    }

                    if (_tag == Tag.Str && rightOperand._tag == Tag.Str)
                    {
                        return new TaggedUnion(AsType<string>() + rightOperand.AsType<string>());
                    }

                    break;
                }
            }

            throw UnsupportedBinary(ExpressionOperator.Add, rightOperand);
        }

        internal TaggedUnion CreateDifference(TaggedUnion rightOperand)
        {
            if (LookupBinary(ExpressionOperator.Subtract, rightOperand) == ConversionCase.ToInt)
            {
                return new TaggedUnion(PromoteToLong() - rightOperand.PromoteToLong());
            }

            if (LookupBinary(ExpressionOperator.Subtract, rightOperand) == ConversionCase.ToFloat)
            {
                return new TaggedUnion(PromoteToDouble() - rightOperand.PromoteToDouble());
            }

            if (LookupBinary(ExpressionOperator.Subtract, rightOperand) == ConversionCase.Nothing)
            {
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }

            throw UnsupportedBinary(ExpressionOperator.Subtract, rightOperand);
        }

        internal TaggedUnion CreateProduct(TaggedUnion rightOperand)
        {
            switch (LookupBinary(ExpressionOperator.Multiply, rightOperand))
            {
                case ConversionCase.ToInt:
                {
                    return new TaggedUnion(PromoteToLong() * rightOperand.PromoteToLong());
                }
                case ConversionCase.ToFloat:
                {
                    return new TaggedUnion(PromoteToDouble() * rightOperand.PromoteToDouble());
                }
                case ConversionCase.Nothing:
                {
                    // Python treats bool as a subtype of int, so [1] * True and "ab" * True are valid.
                    if (_tag == Tag.List && IsIntegerTag(rightOperand._tag))
                    {
                        return new TaggedUnion(InternalList.Repeat(AsType<InternalList>(), rightOperand.AsType<int>()));
                    }

                    if (IsIntegerTag(_tag) && rightOperand._tag == Tag.List)
                    {
                        return new TaggedUnion(InternalList.Repeat(rightOperand.AsType<InternalList>(), AsType<int>()));
                    }

                    if (_tag == Tag.Str && IsIntegerTag(rightOperand._tag))
                    {
                        return new TaggedUnion(RepeatString(AsType<string>(), rightOperand.AsType<int>()));
                    }

                    if (IsIntegerTag(_tag) && rightOperand._tag == Tag.Str)
                    {
                        return new TaggedUnion(RepeatString(rightOperand.AsType<string>(), AsType<int>()));
                    }

                    break;
                }
            }

            throw UnsupportedBinary(ExpressionOperator.Multiply, rightOperand);
        }

        internal TaggedUnion CreateQuotient(TaggedUnion rightOperand)
        {
            // Python semantics: `/` always produces a float, even for int / int.
            switch (LookupBinary(ExpressionOperator.Divide, rightOperand))
            {
                case ConversionCase.ToFloat:
                {
                    var divisor = rightOperand.PromoteToDouble();

                    return divisor == 0.0 
                        ? throw new ZeroDivisionException() 
                        : new TaggedUnion(PromoteToDouble() / divisor);

                }
                case ConversionCase.Nothing:
                case ConversionCase.ToInt:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            throw UnsupportedBinary(ExpressionOperator.Divide, rightOperand);
        }

        internal TaggedUnion CreateModulus(TaggedUnion rightOperand)
        {
            // Python semantics: result has the sign of the divisor.
            switch (LookupBinary(ExpressionOperator.Modulus, rightOperand))
            {
                case ConversionCase.ToInt:
                {
                    var a = PromoteToLong();
                    var b = rightOperand.PromoteToLong();

                    if (b == 0L)
                    {
                        throw new ZeroDivisionException();
                    }

                    return new TaggedUnion((a % b + b) % b);
                }
                case ConversionCase.ToFloat:
                {
                    var l = PromoteToDouble();
                    var r = rightOperand.PromoteToDouble();

                    if (r == 0.0)
                    {
                        throw new ZeroDivisionException();
                    }

                    return new TaggedUnion((l % r + r) % r);
                }
            }

            throw UnsupportedBinary(ExpressionOperator.Modulus, rightOperand);
        }

        internal TaggedUnion CreateFloorQuotient(TaggedUnion rightOperand)
        {
            // Python semantics: floors toward negative infinity. Integer path stays in longs (no detour
            // through double) so values past 2^53 remain exact.
            switch (LookupBinary(ExpressionOperator.FloorDivide, rightOperand))
            {
                case ConversionCase.ToInt:
                {
                    var a = PromoteToLong();
                    var b = rightOperand.PromoteToLong();

                    if (b == 0L)
                    {
                        throw new ZeroDivisionException();
                    }

                    var q = a / b;

                    if (a % b != 0L && a < 0L != b < 0L)
                    {
                        q--;
                    }

                    return new TaggedUnion(q);
                }
                case ConversionCase.ToFloat:
                {
                    var divisor = rightOperand.PromoteToDouble();

                    if (divisor == 0.0)
                    {
                        throw new ZeroDivisionException();
                    }

                    return new TaggedUnion(Math.Floor(PromoteToDouble() / divisor));
                }
            }

            throw UnsupportedBinary(ExpressionOperator.FloorDivide, rightOperand);
        }

        internal TaggedUnion CreatePower(TaggedUnion rightOperand)
        {
            // Python semantics: float if either operand is float, or if exponent is negative.
            // This is the one documented map override: the negative-exponent rule is value-dependent
            // (depends on the runtime exponent's sign), not type-dependent, so it cannot live in the
            // type-keyed map. Every other dispatch path defers to DataTypeConversionMap.
            var conv = LookupBinary(ExpressionOperator.Exponentiate, rightOperand);

            if (conv == ConversionCase.ToInt && rightOperand.PromoteToLong() < 0)
            {
                conv = ConversionCase.ToFloat;
            }

            switch (conv)
            {
                case ConversionCase.ToInt:
                {
                    // Exponent is non-negative here (negative-exp routed to float above). Exact integer
                    // exponentiation avoids the 2^53 precision ceiling of (long)Math.Pow. Overflow wraps
                    // silently — matches prior behavior; arbitrary-precision int is a separate concern.
                    return new TaggedUnion(IntPow(PromoteToLong(), rightOperand.PromoteToLong()));
                }
                case ConversionCase.ToFloat:
                {
                    return new TaggedUnion(Math.Pow(PromoteToDouble(), rightOperand.PromoteToDouble()));
                }
            }

            throw UnsupportedBinary(ExpressionOperator.Exponentiate, rightOperand);
        }

        internal TaggedUnion CreateUnion(TaggedUnion rightOperand)
        {
            if (LookupBinary(ExpressionOperator.BinaryOr, rightOperand) == ConversionCase.Nothing
                && _tag == Tag.Dict && rightOperand._tag == Tag.Dict)
            {
                return new TaggedUnion(InternalDict.Merge(AsType<InternalDict>(), rightOperand.AsType<InternalDict>()));
            }

            throw UnsupportedBinary(ExpressionOperator.BinaryOr, rightOperand);
        }

        internal TaggedUnion CreateNegation()
        {
            switch (LookupUnary(ExpressionOperator.Negate))
            {
                case ConversionCase.ToInt:
                {
                    return new TaggedUnion(-PromoteToLong());
                }
                case ConversionCase.ToFloat:
                {
                    return new TaggedUnion(-PromoteToDouble());
                }
            }

            throw UnsupportedUnary(ExpressionOperator.Negate);
        }

        internal TaggedUnion CreateLogicalNot()
        {
            // The map records this as Nothing for every type; consult it for consistency and so that
            // a future map change (e.g. restricting unary `not` to specific types) propagates here.
            LookupUnary(ExpressionOperator.Not);
            return new TaggedUnion(!IsTruthy());
        }

        internal TaggedUnion CreateStr()
        {
            LookupUnary(ExpressionOperator.ToStr);
            return new TaggedUnion(ToStr());
        }

        #endregion

        #region Comparison Operations

        internal bool IsTypeAgnosticEqualTo(TaggedUnion other)
        {
            switch (LookupBinary(ExpressionOperator.Equal, other))
            {
                case ConversionCase.ToInt:
                    return PromoteToLong() == other.PromoteToLong();
                case ConversionCase.ToFloat:
                {
                    return PromoteToDouble() == other.PromoteToDouble();
                }
                case ConversionCase.Nothing:
                {
                    return EqualsNoConversion(other);
                }
            }

            return false;
        }

        internal bool IsNotEqualTo(TaggedUnion other)
        {
            switch (LookupBinary(ExpressionOperator.NotEqual, other))
            {
                case ConversionCase.ToInt:
                {
                    return PromoteToLong() != other.PromoteToLong();
                }
                case ConversionCase.ToFloat:
                {
                    return PromoteToDouble() != other.PromoteToDouble();
                }
                case ConversionCase.Nothing:
                {
                    return !EqualsNoConversion(other);
                }
            }

            return true;
        }

        internal bool IsLessThan(TaggedUnion other)
        {
            switch (LookupBinary(ExpressionOperator.Less, other))
            {
                case ConversionCase.ToInt:
                {
                    return PromoteToLong() < other.PromoteToLong();
                }
                case ConversionCase.ToFloat:
                {
                    return PromoteToDouble() < other.PromoteToDouble();
                }
                case ConversionCase.Nothing:
                {
                    if (_tag == Tag.Str && other._tag == Tag.Str)
                    {
                        return string.CompareOrdinal(AsType<string>(), other.AsType<string>()) < 0;
                    }

                    break;
                }
            }

            throw UnsupportedBinary(ExpressionOperator.Less, other);
        }

        internal bool IsGreaterThan(TaggedUnion other)
        {
            switch (LookupBinary(ExpressionOperator.Greater, other))
            {
                case ConversionCase.ToInt:
                {
                    return PromoteToLong() > other.PromoteToLong();
                }
                case ConversionCase.ToFloat:
                {
                    return PromoteToDouble() > other.PromoteToDouble();
                }
                case ConversionCase.Nothing:
                {
                    if (_tag == Tag.Str && other._tag == Tag.Str)
                    {
                        return string.CompareOrdinal(AsType<string>(), other.AsType<string>()) > 0;
                    }

                    break;
                }
            }

            throw UnsupportedBinary(ExpressionOperator.Greater, other);
        }

        internal bool IsLessOrEqualTo(TaggedUnion other)
        {
            switch (LookupBinary(ExpressionOperator.LessEqual, other))
            {
                case ConversionCase.ToInt:
                {
                    return PromoteToLong() <= other.PromoteToLong();
                }
                case ConversionCase.ToFloat:
                {
                    return PromoteToDouble() <= other.PromoteToDouble();
                }
                case ConversionCase.Nothing:
                {
                    if (_tag == Tag.Str && other._tag == Tag.Str)
                    {
                        return string.CompareOrdinal(AsType<string>(), other.AsType<string>()) <= 0;
                    }

                    break;
                }
            }

            throw UnsupportedBinary(ExpressionOperator.LessEqual, other);
        }

        internal bool IsGreaterOrEqualTo(TaggedUnion other)
        {
            switch (LookupBinary(ExpressionOperator.GreaterEqual, other))
            {
                case ConversionCase.ToInt:
                {
                    return PromoteToLong() >= other.PromoteToLong();
                }
                case ConversionCase.ToFloat:
                {
                    return PromoteToDouble() >= other.PromoteToDouble();
                }
                case ConversionCase.Nothing:
                {
                    if (_tag == Tag.Str && other._tag == Tag.Str)
                    {
                        return string.CompareOrdinal(AsType<string>(), other.AsType<string>()) >= 0;
                    }

                    break;
                }
            }

            throw UnsupportedBinary(ExpressionOperator.GreaterEqual, other);
        }

        #endregion

        #region Interop

        internal TaggedUnion InvokeHostDelegate(TaggedUnion[] args)
        {
            if (_tag != Tag.Object)
            {
                throw new TypeException($"'{_tag}' object is not callable");
            }

            if (_obj is Func<TaggedUnion[], TaggedUnion> methodDelegate)
            {
                return methodDelegate(args ?? Array.Empty<TaggedUnion>());
            }

            throw new InvalidOperationException($"Object of type '{_obj.GetType().Name}' is not callable");
        }

        #endregion

        #region Object Overrides

        /// <summary>
        /// Strict structural equality: returns <c>true</c> only when <paramref name="obj"/> is a
        /// <see cref="TaggedUnion"/> of the same <see cref="_tag"/> with the same underlying value.
        /// No cross-type conversion is performed — <c>True</c> is not equal to <c>1</c>, and
        /// <c>1</c> is not equal to <c>1.0</c>. For Python <c>==</c> semantics that promote across
        /// numeric types, use <see cref="IsTypeAgnosticEqualTo"/> instead.
        /// </summary>
        /// <param name="obj">The object to compare with this instance or <c>null</c>.</param>
        /// <returns><c>true</c> if the objects are structurally equal; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return obj is TaggedUnion other && EqualsNoConversion(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int valueHash;

            switch (_tag)
            {
                case Tag.Long:
                {
                    valueHash = _long.GetHashCode();
                    break;
                }
                case Tag.Double:
                {
                    valueHash = _dbl.GetHashCode();
                    break;
                }
                case Tag.Bool:
                {
                    valueHash = BoolValue.GetHashCode();
                    break;
                }
                case Tag.List:
                {
                    valueHash = InternalList.ElementsHashCode((InternalList)_obj);
                    break;
                }
                case Tag.Dict:
                {
                    valueHash = InternalDict.ElementsHashCode((InternalDict)_obj);
                    break;
                }
                default:
                {
                    valueHash = _obj?.GetHashCode() ?? 0;
                    break;
                }
            }

            unchecked
            {
                return _tag.GetHashCode() * HASH_COMBINE_PRIME ^ valueHash;
            }
        }

        /// <summary>
        /// Returns a string representation of this instance with the same format as a Chow <b>str</b> value.
        /// </summary>
        public override string ToString()
        {
            return ToStr();
        }

        #endregion

        #region Conversion Methods

        // NOTE: These methods are for internal use only and are more performant than AsType<T>()
        // that is intended for the library's client

        internal bool ToBool()
        {
            switch (_tag)
            {
                case Tag.None:
                {
                    return NONE_TO_BOOL_T_AND_F;
                }
                case Tag.Bool:
                {
                    return BoolValue;
                }
                case Tag.Object:
                {
                    return OBJ_TO_BOOL_T_AND_F;
                }
                case Tag.Long:
                {
                    return _long != LONG_TO_BOOL_F;
                }
                case Tag.Double:
                {
                    return Math.Abs(_dbl - DBL_TO_BOOL_F) > TOLERANCE;
                }
                case Tag.Str:
                {
                    return StrToBool();
                }
                case Tag.List:
                {
                    return ListToBool();
                }
                case Tag.Dict:
                {
                    return DictToBool();
                }
                case Tag.Range:
                {
                    return RangeToBool();
                }
            }

            throw new InvalidOperationException();
        }

        const double TOLERANCE = 0.000000000000001;

        internal long ToLong()
        {
            switch (_tag)
            {
                case Tag.None:
                {
                    throw new InvalidOperationException("Cannot convert None to long");
                }
                case Tag.Bool:
                {
                    return BoolValue ? BOOL_T_TO_LONG : BOOL_F_TO_LONG;
                }
                case Tag.Long:
                {
                    return _long;
                }
                case Tag.Double:
                {
                    return (long)_dbl;
                }
                case Tag.Str:
                {
                    return StrToLong();
                }
                default:
            throw new InvalidOperationException();
                    
            }

        }

        internal double ToDouble()
        {
            switch (_tag)
            {
                case Tag.None:
                {
                    throw new InvalidOperationException("Cannot convert None to double");
                }
                case Tag.Bool:
                {
                    return BoolValue ? BOOL_T_TO_DBL : BOOL_F_TO_DBL;
                }
                case Tag.Long:
                {
                    return _long;
                }
                case Tag.Double:
                {
                    return _dbl;
                }
                case Tag.Str:
                {
                    return StrToDouble();
                }
                case Tag.Object:
                {
                    throw new InvalidOperationException("Cannot convert Object to double");
                }
                case Tag.List:
                {
                    throw new InvalidOperationException("Cannot convert List to double");
                }
                case Tag.Dict:
                {
                    throw new InvalidOperationException("Cannot convert Dict to double");
                }
                case Tag.Range:
                {
                    throw new InvalidOperationException("Cannot convert Range to double");
                }
            }

            throw new InvalidOperationException();
        }

        internal object ToObject()
        {
            switch (_tag)
            {
                case Tag.None:
                {
                    return null;
                }
                case Tag.Bool:
                {
                    return BoolValue;
                }
                case Tag.Long:
                {
                    return _long;
                }
                case Tag.Double:
                {
                    return _dbl;
                }
                case Tag.Str:
                case Tag.Object:
                case Tag.List:
                case Tag.Dict:
                case Tag.Range:
                {
                    return _obj;
                }
            }

            throw new InvalidOperationException();
        }

        internal string ToStr()
        {
            switch (_tag)
            {
                case Tag.None:
                {
                    return NONE_TO_STR;
                }
                case Tag.Bool:
                {
                    return BoolValue ? BOOL_T_TO_STR : BOOL_F_TO_STR;
                }
                case Tag.Long:
                {
                    return _long.ToString(CultureInfo.InvariantCulture);
                }
                case Tag.Double:
                {
                    return FloatToStr();
                }
                case Tag.Str:
                {
                    return StrToStr();
                }
                case Tag.Object:
                case Tag.List:
                case Tag.Dict:
                case Tag.Range:
                {
                    if (_obj == null)
                    {
                        // This should never happen, but we'll check just in case
                        throw new InvalidOperationException($"{nameof(TaggedUnion)} object with type {_tag} null");
                    }

                    return _obj.ToString();
                }
            }

            throw new InvalidOperationException();
        }

        #endregion

        #region Conversion Helpers

        bool StrToBool()
        {
            if (_obj is string strValue)
            {
                return strValue.Length != STR_LEN_TO_BOOL_F;
            }

            throw new InvalidOperationException("Expected string value for boolean comparison");
        }

        bool ListToBool()
        {
            if (_obj is InternalList listValue)
            {
                return listValue.Count != LIST_COUNT_TO_BOOL_F;
            }

            throw new InvalidOperationException("Expected list value for boolean comparison");
        }

        bool DictToBool()
        {
            if (_obj is InternalDict dictValue)
            {
                return dictValue.Count != DICT_LEN_TO_BOOL_F;
            }

            throw new InvalidOperationException("Expected dict value for boolean comparison");
        }

        bool RangeToBool()
        {
            if (_obj is InternalRange rangeValue)
            {
                return rangeValue.Count != RNG_LEN_TO_BOOL_F;
            }

            throw new InvalidOperationException("Expected range value for boolean comparison");
        }

        long StrToLong()
        {
            if (!(_obj is string strValue))
            {
                throw new InvalidOperationException("Expected string value for long conversion");
            }

            if (long.TryParse(strValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedLong))
            {
                return parsedLong;
            }

            throw new InvalidOperationException($"Cannot convert string '{strValue}' to long");
        }

        double StrToDouble()
        {
            if (!(_obj is string strValue))
            {
                throw new InvalidOperationException("Expected string value for double conversion");
            }

            if (double.TryParse(strValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedDouble))
            {
                return parsedDouble;
            }

            throw new InvalidOperationException($"Cannot convert string '{strValue}' to double");
        }

        string FloatToStr()
        {
            var formatted = _dbl.ToString(CultureInfo.InvariantCulture);

            if (IsFractionalSuffix(formatted))
            {
                formatted += DBL_LONG_FRACTION_SUFFIX;
            }

            return formatted;
        }

        static bool IsFractionalSuffix(string formatted)
        {
            return formatted.IndexOf(DBL_POINT_CHAR) == CHAR_NOT_FOUND_INX
                && formatted.IndexOf(DBL_POW_LOWER_CHAR) == CHAR_NOT_FOUND_INX
                && formatted.IndexOf(DBL_POW_UPPER_CHAR) == CHAR_NOT_FOUND_INX;
        }

        string StrToStr()
        {
            if (_obj is string strValue)
            {
                return strValue;
            }

            throw new InvalidOperationException("Expected string value for string conversion");
        }

        #endregion

        #region Operator Dispatch Helpers

        // Promotion-rule lookup and result-coercion helpers used by the arithmetic/comparison instance
        // methods above. Operand promotion is intentionally limited to the three numeric tags
        // (Bool/Long/Double); the map guarantees ToInt/ToFloat is only reported for those.

        ConversionCase LookupBinary(ExpressionOperator @operator, TaggedUnion right)
        {
            return DataTypeConversionMap.GetLeftRightConversionCase(@operator, _tag, right._tag);
        }

        ConversionCase LookupUnary(ExpressionOperator @operator)
        {
            return DataTypeConversionMap.GetOperandConversionCase(@operator, _tag);
        }

        TypeException UnsupportedBinary(ExpressionOperator @operator, TaggedUnion right)
        {
            return new TypeException($"unsupported operand type(s) for {@operator}: '{_tag}' and '{right._tag}'");
        }

        TypeException UnsupportedUnary(ExpressionOperator @operator)
        {
            return new TypeException($"bad operand type for unary {@operator}: '{_tag}'");
        }

        // TODO: These are redundant, the ToX methods should be used instead.
        long PromoteToLong()
        {
            switch (_tag)
            {
                case Tag.Bool:
                {
                    return BoolValue ? 1L : 0L;
                }
                case Tag.Long:
                {
                    return _long;
                }
                default:
                {
                    throw new InvalidOperationException($"Cannot promote {_tag} to int");
                }
            }
        }

        double PromoteToDouble()
        {
            switch (_tag)
            {
                case Tag.Bool:
                {
                    return BoolValue ? 1.0 : 0.0;
                }
                case Tag.Long:
                {
                    return _long;
                }
                case Tag.Double:
                {
                    return _dbl;
                }
                default:
                {
                    throw new InvalidOperationException($"Cannot promote {_tag} to float");
                }
            }
        }

        // Equality fallback used when DataTypeConversionMap reports Nothing for ==/!= operands.
        // Cross-type combinations are never equal (Python: 1 == "1" → False); same-type combinations
        // delegate to the underlying value's identity/structural equality.
        bool EqualsNoConversion(TaggedUnion other)
        {
            if (_tag != other._tag)
            {
                return false;
            }

            switch (_tag)
            {
                case Tag.None:
                {
                    return true;
                }
                case Tag.Bool:
                {
                    return BoolValue == other.BoolValue;
                }
                case Tag.Long:
                {
                    return _long == other._long;
                }
                case Tag.Double:
                {
                    return _dbl.Equals(other._dbl);
                }
                case Tag.Str:
                {
                    return (string)_obj == (string)other._obj;
                }
                case Tag.List:
                {
                    return InternalList.ElementsEqual((InternalList)_obj, (InternalList)other._obj);
                }
                case Tag.Dict:
                {
                    return InternalDict.ElementsEqual((InternalDict)_obj, (InternalDict)other._obj);
                }
                case Tag.Range:
                case Tag.Object:
                {
                    return ReferenceEquals(_obj, other._obj);
                }
                default:
                {
                    return false;
                }
            }
        }

        static string RepeatString(string source, int count)
        {
            if (count <= 0 || source.Length == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder(source.Length * count);

            for (var index = 0; index < count; index++)
            {
                builder.Append(source);
            }

            return builder.ToString();
        }

        // Bool is treated as a subtype of Long for container-repeat dispatch (Python parity).
        static bool IsIntegerTag(Tag tag)
        {
            return tag == Tag.Long || tag == Tag.Bool;
        }

        // TODO: Make it so an overflow throws an exception instead of wrapping silently.
        // Exponent-by-squaring. Caller guarantees exponent >= 0 (negative exponents are promoted to
        // float by CreatePower before this is reached). Overflow wraps silently.
        static long IntPow(long b, long e)
        {
            var result = 1L;

            while (e > 0L)
            {
                if ((e & 1L) == 1L)
                {
                    result *= b;
                }

                b *= b;
                e >>= 1;
            }

            return result;
        }

        #endregion

        #region Constants

        // GetHashCode mixing multiplier (standard small prime for combining hashes)
        const int HASH_COMBINE_PRIME = 397;

        // Constructor defaults
        const bool NOT_BOOL_INIT = false;
        const object NOT_OBJ_INIT = null;
        const long NOT_LONG_INIT = 0L;
        const double NOT_DBL_INIT = 0.0;

        // ToBool source representations (numeric "false" values)
        const long LONG_TO_BOOL_F = 0L;
        const double DBL_TO_BOOL_F = 0.0;

        // ToBool source representations (container/string "false" lengths, plus None/Object fixed reps)
        const int STR_LEN_TO_BOOL_F = 0;
        const int LIST_COUNT_TO_BOOL_F = 0;
        const int DICT_LEN_TO_BOOL_F = 0;
        const int RNG_LEN_TO_BOOL_F = 0;
        const bool NONE_TO_BOOL_T_AND_F = false;
        const bool OBJ_TO_BOOL_T_AND_F = true;

        // ToLong source representations (bool -> long)
        const long BOOL_F_TO_LONG = 0L;
        const long BOOL_T_TO_LONG = 1L;

        // ToDouble source representations (bool -> double)
        const double BOOL_F_TO_DBL = 0.0;
        const double BOOL_T_TO_DBL = 1.0;

        // ToStr source representations (None/bool -> str)
        const string NONE_TO_STR = "None";
        const string BOOL_F_TO_STR = "False";
        const string BOOL_T_TO_STR = "True";

        // ToStr float formatting (append ".0" when ToString output has no decimal point or pow)
        const string DBL_LONG_FRACTION_SUFFIX = ".0";
        const char DBL_POINT_CHAR = '.';
        const char DBL_POW_LOWER_CHAR = 'e';
        const char DBL_POW_UPPER_CHAR = 'E';

        // String.IndexOf "not found" sentinel (used by ToStr float formatting check)
        const int CHAR_NOT_FOUND_INX = -1;

        const int OBJ_FIELD_OFFSET = 0;
        const int LONG_FIELD_OFFSET = 8;
        const int DBL_FIELD_OFFSET = 8;
        const int TAG_FIELD_OFFSET = 16;

        #endregion

    }
}
