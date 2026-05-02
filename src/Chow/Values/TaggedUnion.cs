using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Values
{
    struct TaggedUnion
    {
        const float DEFAULT_FLOAT_VALUE = 0.0f;
        const int DEFAULT_INT_VALUE = 0;

        static bool _isEmptyUnionInitialized = false;
        static TaggedUnion _emptyUnion;

        TaggedUnionType _type;
        int _intValue;
        float _floatValue;

        public static TaggedUnion Empty = new TaggedUnion(TaggedUnionType.Empty);

        public TaggedUnionType Type => _type;

        public bool IsEmpty => _type == TaggedUnionType.Empty;
        public bool IsInteger => _type == TaggedUnionType.Integer;
        public bool IsFloat => _type == TaggedUnionType.Float;

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

        private TaggedUnion(TaggedUnionType type)
        {
            _type = type;
            _intValue = DEFAULT_INT_VALUE;
            _floatValue = DEFAULT_FLOAT_VALUE;
        }

        public TaggedUnion(float value)
        {
            _floatValue = value;
            _type = TaggedUnionType.Float; 
            _intValue = DEFAULT_INT_VALUE;
        }

        public TaggedUnion(int value)
        {
            _intValue = value;
            _type = TaggedUnionType.Integer;
            _floatValue = DEFAULT_FLOAT_VALUE;
        }



        public static TaggedUnion operator +(TaggedUnion left, TaggedUnion right)
        {
            if (EitherIsFloat(left, right))
            {
                return new TaggedUnion(AsFloat(left) + AsFloat(right));
            }

            return new TaggedUnion(left.IntegerValue + right.IntegerValue);
        }

        public static TaggedUnion operator -(TaggedUnion left, TaggedUnion right)
        {
            if (EitherIsFloat(left, right))
            {
                return new TaggedUnion(AsFloat(left) - AsFloat(right));
            }

            return new TaggedUnion(left.IntegerValue - right.IntegerValue);
        }

        public static TaggedUnion operator *(TaggedUnion left, TaggedUnion right)
        {
            if (EitherIsFloat(left, right))
            {
                return new TaggedUnion(AsFloat(left) * AsFloat(right));
            }

            return new TaggedUnion(left.IntegerValue * right.IntegerValue);
        }

        public static TaggedUnion operator /(TaggedUnion left, TaggedUnion right)
        {
            if (EitherIsFloat(left, right))
            {
                return new TaggedUnion(AsFloat(left) / AsFloat(right));
            }

            return new TaggedUnion(left.IntegerValue / right.IntegerValue);
        }

        static bool EitherIsFloat(TaggedUnion left, TaggedUnion right)
        {
            return left.IsFloat || right.IsFloat;
        }

        static float AsFloat(TaggedUnion union)
        {
            if (union.IsFloat)
            {
                return union.FloatValue;
            }

            return union.IntegerValue;
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
