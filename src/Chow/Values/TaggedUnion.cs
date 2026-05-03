using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Values
{
    struct TaggedUnion
    {
        const float DEFAULT_FLOAT_VALUE = 0.0f;
        const int DEFAULT_INT_VALUE = 0;
        const string DEFAULT_STRING_VALUE = null;

        TaggedUnionType _type;
        int _intValue;
        float _floatValue;
        string _stringValue;

        public static TaggedUnion Empty = new TaggedUnion(TaggedUnionType.Empty);
        public static TaggedUnion None = new TaggedUnion(TaggedUnionType.None);

        public TaggedUnionType Type => _type;

        public bool IsEmpty => _type == TaggedUnionType.Empty;
        public bool IsNone => _type == TaggedUnionType.None;
        public bool IsInteger => _type == TaggedUnionType.Integer;
        public bool IsFloat => _type == TaggedUnionType.Float;
        public bool IsString => _type == TaggedUnionType.String;

        public int IntegerValue
        {
            get
            {
                ValidateTaggedUnionType(TaggedUnionType.Integer);
                return _intValue;
            }
            set
            {
                ValidateTaggedUnionType(TaggedUnionType.Integer);
                _intValue = value;
            }
        }

        public float FloatValue
        {
            get
            {
                ValidateTaggedUnionType(TaggedUnionType.Float);
                return _floatValue;
            }
            set
            {
                ValidateTaggedUnionType(TaggedUnionType.Float);
                _floatValue = value;
            }
        }

        public string StringValue
        {
            get
            {
                ValidateTaggedUnionType(TaggedUnionType.String);
                return _stringValue;
            }
            set
            {
                ValidateTaggedUnionType(TaggedUnionType.String);
                _stringValue = value;
            }
        }

        private TaggedUnion(TaggedUnionType type)
        {
            _type = type;
            _intValue = DEFAULT_INT_VALUE;
            _floatValue = DEFAULT_FLOAT_VALUE;
            _stringValue = DEFAULT_STRING_VALUE;
        }

        public TaggedUnion(float value)
        {
            _floatValue = value;
            _type = TaggedUnionType.Float; 
            _intValue = DEFAULT_INT_VALUE;
            _stringValue = DEFAULT_STRING_VALUE;
        }

        public TaggedUnion(int value)
        {
            _intValue = value;
            _type = TaggedUnionType.Integer;
            _floatValue = DEFAULT_FLOAT_VALUE;
            _stringValue = DEFAULT_STRING_VALUE;
        }

        public TaggedUnion(string value)
        {
            _stringValue = value;
            _type = TaggedUnionType.String;
            _intValue = DEFAULT_INT_VALUE;
            _floatValue = DEFAULT_FLOAT_VALUE;
        }



        public static TaggedUnion operator +(TaggedUnion left, TaggedUnion right)
        {
            ThrowIfStringOperands(left, right);
            if (EitherIsFloat(left, right))
            {
                return new TaggedUnion(AsFloat(left) + AsFloat(right));
            }

            return new TaggedUnion(left.IntegerValue + right.IntegerValue);
        }

        public static TaggedUnion operator -(TaggedUnion left, TaggedUnion right)
        {
            ThrowIfStringOperands(left, right);
            if (EitherIsFloat(left, right))
            {
                return new TaggedUnion(AsFloat(left) - AsFloat(right));
            }

            return new TaggedUnion(left.IntegerValue - right.IntegerValue);
        }

        public static TaggedUnion operator *(TaggedUnion left, TaggedUnion right)
        {
            ThrowIfStringOperands(left, right);
            if (EitherIsFloat(left, right))
            {
                return new TaggedUnion(AsFloat(left) * AsFloat(right));
            }

            return new TaggedUnion(left.IntegerValue * right.IntegerValue);
        }

        public static TaggedUnion operator /(TaggedUnion left, TaggedUnion right)
        {
            ThrowIfStringOperands(left, right);
            // Python semantics: `/` always produces a float, even for int / int.
            return new TaggedUnion(AsFloat(left) / AsFloat(right));
        }

        public static TaggedUnion operator %(TaggedUnion left, TaggedUnion right)
        {
            ThrowIfStringOperands(left, right);
            // Python semantics: result has the sign of the divisor.
            if (EitherIsFloat(left, right))
            {
                float l = AsFloat(left);
                float r = AsFloat(right);
                return new TaggedUnion(((l % r) + r) % r);
            }

            int a = left.IntegerValue;
            int b = right.IntegerValue;
            return new TaggedUnion(((a % b) + b) % b);
        }

        public static TaggedUnion FloorDivide(TaggedUnion left, TaggedUnion right)
        {
            ThrowIfStringOperands(left, right);
            // Python semantics: floors toward negative infinity.
            if (EitherIsFloat(left, right))
            {
                return new TaggedUnion((float)Math.Floor(AsFloat(left) / (double)AsFloat(right)));
            }

            return new TaggedUnion((int)Math.Floor(left.IntegerValue / (double)right.IntegerValue));
        }

        public static TaggedUnion Power(TaggedUnion left, TaggedUnion right)
        {
            ThrowIfStringOperands(left, right);
            // Python semantics: float if either operand is float, or if exponent is negative.
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
                case TaggedUnionType.Integer:
                    return left._intValue == right._intValue;
                case TaggedUnionType.Float:
                    return left._floatValue == right._floatValue;
                case TaggedUnionType.String:
                    return left._stringValue == right._stringValue;
                default:
                    return true;
            }
        }

        public static bool operator !=(TaggedUnion left, TaggedUnion right)
        {
            return !(left == right);
        }

        static bool EitherIsFloat(TaggedUnion left, TaggedUnion right)
        {
            return left.IsFloat || right.IsFloat;
        }

        static void ThrowIfStringOperands(TaggedUnion left, TaggedUnion right)
        {
            if (left.IsString || right.IsString)
            {
                throw new InvalidOperationException("String operands are not supported for this operation.");
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
                case TaggedUnionType.Integer:
                    return _intValue.GetHashCode();
                case TaggedUnionType.Float:
                    return _floatValue.GetHashCode();
                case TaggedUnionType.String:
                    return _stringValue?.GetHashCode() ?? 0;
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

            return $"TaggedUnion(type={_type}, value={_intValue})";
        }

        void ValidateTaggedUnionType(TaggedUnionType desiredType)
        {
            if (_type == desiredType)
            {
                return;
            }

            throw new InvalidOperationException($"{desiredType} access attempt but union's type is {_type}");
        }
    }
}
