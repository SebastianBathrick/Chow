using Chow.Interpreter.State.Values;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Chow.Interpreter
{
    internal enum BuiltInType
    {
        Print,
        Input,
        Float,
        Str,
        Int,
        Bool,
        List,
        Dict,
        Len,
        Type,
        Abs,
        Round,
        Min,
        Max,
        Range,
    }

    internal static class BuiltIns
    {
        static readonly Dictionary<BuiltInType, string> _names = new Dictionary<BuiltInType, string>
        {
            { BuiltInType.Print, "print" },
            { BuiltInType.Input, "input" },
            { BuiltInType.Float, "float" },
            { BuiltInType.Str,   "str"   },
            { BuiltInType.Int,   "int"   },
            { BuiltInType.Bool,  "bool"  },
            { BuiltInType.List,  "list"  },
            { BuiltInType.Dict,  "dict"  },
            { BuiltInType.Len,   "len"   },
            { BuiltInType.Type,  "type"  },
            { BuiltInType.Abs,   "abs"   },
            { BuiltInType.Round, "round" },
            { BuiltInType.Min,   "min"   },
            { BuiltInType.Max,   "max"   },
            { BuiltInType.Range, "range" },
        };

        static readonly Dictionary<BuiltInType, Func<TaggedUnion[], TaggedUnion>> _defaults =
            new Dictionary<BuiltInType, Func<TaggedUnion[], TaggedUnion>>
        {
            { BuiltInType.Print, Print },
            { BuiltInType.Input, Input },
            { BuiltInType.Float, Float },
            { BuiltInType.Str,   Str   },
            { BuiltInType.Int,   Int   },
            { BuiltInType.Bool,  Bool  },
            { BuiltInType.List,  List  },
            { BuiltInType.Dict,  Dict  },
            { BuiltInType.Len,   Len   },
            { BuiltInType.Type,  Type  },
            { BuiltInType.Abs,   Abs   },
            { BuiltInType.Round, Round },
            { BuiltInType.Min,   Min   },
            { BuiltInType.Max,   Max   },
            { BuiltInType.Range, Range },
        };

        public static IEnumerable<BuiltInType> AllTypes => _names.Keys;

        public static string NameOf(BuiltInType type)
        {
            return _names[type];
        }

        public static Func<TaggedUnion[], TaggedUnion> DefaultOf(BuiltInType type)
        {
            return _defaults[type];
        }

        static TaggedUnion Print(TaggedUnion[] args)
        {
            RequireArity("print", args, 1);
            Console.WriteLine(FormatForPrint(args[0]));
            return TaggedUnion.None;
        }

        static TaggedUnion Input(TaggedUnion[] args)
        {
            RequireArity("input", args, 0);

            var input = Console.ReadLine();

            if (input == null)
            {
                input = string.Empty;
            }

            return new TaggedUnion(input);
        }

        static TaggedUnion Float(TaggedUnion[] args)
        {
            RequireArity("float", args, 1);
            var val = args[0];

            switch (val.Tag)
            {
                case Tag.Float:
                {
                    return val;
                }
                case Tag.Int:
                {
                    return new TaggedUnion((double)val.IntegerValue);
                }
                case Tag.Boolean:
                {
                    return new TaggedUnion(val.BooleanValue ? 1.0 : 0.0);
                }
                case Tag.Str:
                {
                    double parsed;
                    if (double.TryParse(val.StringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                    {
                        return new TaggedUnion(parsed);
                    }
                    throw new InvalidOperationException($"could not convert string to float: '{val.StringValue}'");
                }
            }

            throw new InvalidOperationException($"float() argument must be a string or a number, not '{TagTypeName(val.Tag)}'");
        }

        static TaggedUnion Str(TaggedUnion[] args)
        {
            RequireArity("str", args, 1);
            return new TaggedUnion(FormatForPrint(args[0]));
        }

        static TaggedUnion Int(TaggedUnion[] args)
        {
            RequireArity("int", args, 1);
            var val = args[0];

            switch (val.Tag)
            {
                case Tag.Int:
                {
                    return val;
                }
                case Tag.Float:
                {
                    return new TaggedUnion((long)val.FloatValue);
                }
                case Tag.Boolean:
                {
                    return new TaggedUnion(val.BooleanValue ? 1L : 0L);
                }
                case Tag.Str:
                {
                    long parsed;
                    if (long.TryParse(val.StringValue, out parsed))
                    {
                        return new TaggedUnion(parsed);
                    }
                    throw new InvalidOperationException($"invalid literal for int() with base 10: '{val.StringValue}'");
                }
            }

            throw new InvalidOperationException($"int() argument must be a string, a bytes-like object or a real number, not '{TagTypeName(val.Tag)}'");
        }

        static TaggedUnion Bool(TaggedUnion[] args)
        {
            RequireArity("bool", args, 1);
            return new TaggedUnion(args[0].IsTruthy);
        }

        static TaggedUnion List(TaggedUnion[] args)
        {
            var argCount = args == null ? 0 : args.Length;

            if (argCount == 0)
            {
                return new TaggedUnion(new InternalList());
            }

            if (argCount == 1)
            {
                if (args[0].Tag == Tag.List)
                {
                    return new TaggedUnion(InternalList.Concat(args[0].ListValue, new InternalList()));
                }
                throw new InvalidOperationException($"'{TagTypeName(args[0].Tag)}' object is not iterable");
            }

            throw new InvalidOperationException($"list expected at most 1 argument, got {argCount}");
        }

        static TaggedUnion Dict(TaggedUnion[] args)
        {
            var argCount = args == null ? 0 : args.Length;

            if (argCount == 0)
            {
                return new TaggedUnion(new InternalDict());
            }

            if (argCount == 1)
            {
                if (args[0].Tag == Tag.Dict)
                {
                    return new TaggedUnion(InternalDict.Merge(args[0].DictValue, new InternalDict()));
                }
                throw new InvalidOperationException($"'{TagTypeName(args[0].Tag)}' object is not iterable");
            }

            throw new InvalidOperationException($"dict expected at most 1 argument, got {argCount}");
        }

        static TaggedUnion Len(TaggedUnion[] args)
        {
            RequireArity("len", args, 1);
            var val = args[0];

            switch (val.Tag)
            {
                case Tag.Str:
                {
                    return new TaggedUnion((long)val.StringValue.Length);
                }
                case Tag.List:
                {
                    return new TaggedUnion((long)val.ListValue.Count);
                }
                case Tag.Dict:
                {
                    return new TaggedUnion((long)val.DictValue.Count);
                }
            }

            throw new InvalidOperationException($"object of type '{TagTypeName(val.Tag)}' has no len()");
        }

        static TaggedUnion Type(TaggedUnion[] args)
        {
            RequireArity("type", args, 1);
            return new TaggedUnion(TagTypeName(args[0].Tag));
        }

        static TaggedUnion Abs(TaggedUnion[] args)
        {
            RequireArity("abs", args, 1);
            var val = args[0];

            switch (val.Tag)
            {
                case Tag.Int:
                {
                    return new TaggedUnion(Math.Abs(val.IntegerValue));
                }
                case Tag.Float:
                {
                    return new TaggedUnion(Math.Abs(val.FloatValue));
                }
                case Tag.Boolean:
                {
                    return new TaggedUnion(val.BooleanValue ? 1L : 0L);
                }
            }

            throw new InvalidOperationException($"bad operand type for abs(): '{TagTypeName(val.Tag)}'");
        }

        static TaggedUnion Round(TaggedUnion[] args)
        {
            RequireArity("round", args, 1);
            var val = args[0];

            switch (val.Tag)
            {
                case Tag.Int:
                {
                    return val;
                }
                case Tag.Float:
                {
                    return new TaggedUnion((long)Math.Round(val.FloatValue, MidpointRounding.ToEven));
                }
                case Tag.Boolean:
                {
                    return new TaggedUnion(val.BooleanValue ? 1L : 0L);
                }
            }

            throw new InvalidOperationException($"type {TagTypeName(val.Tag)} doesn't define __round__ method");
        }

        static TaggedUnion Min(TaggedUnion[] args)
        {
            RequireArity("min", args, 2);

            if (!IsNumeric(args[0]) || !IsNumeric(args[1]))
            {
                throw new InvalidOperationException("min() arguments must be numbers");
            }

            return AsDouble(args[0]) <= AsDouble(args[1]) ? args[0] : args[1];
        }

        static TaggedUnion Max(TaggedUnion[] args)
        {
            RequireArity("max", args, 2);

            if (!IsNumeric(args[0]) || !IsNumeric(args[1]))
            {
                throw new InvalidOperationException("max() arguments must be numbers");
            }

            return AsDouble(args[0]) >= AsDouble(args[1]) ? args[0] : args[1];
        }

        static TaggedUnion Range(TaggedUnion[] args)
        {
            var argCount = args == null ? 0 : args.Length;

            if (argCount == 0 || argCount > 3)
            {
                throw new InvalidOperationException($"range expected 1 to 3 arguments, got {argCount}");
            }

            long start;
            long stop;
            long step;

            if (argCount == 1)
            {
                start = 0;
                stop = RequireRangeInt(args[0], 0);
                step = 1;
            }
            else if (argCount == 2)
            {
                start = RequireRangeInt(args[0], 0);
                stop = RequireRangeInt(args[1], 1);
                step = 1;
            }
            else
            {
                start = RequireRangeInt(args[0], 0);
                stop = RequireRangeInt(args[1], 1);
                step = RequireRangeInt(args[2], 2);

                if (step == 0)
                {
                    throw new InvalidOperationException("range() arg 3 must not be zero");
                }
            }

            return new TaggedUnion(new InternalRange(start, stop, step));
        }

        static long RequireRangeInt(TaggedUnion arg, int position)
        {
            if (arg.Tag != Tag.Int)
            {
                throw new InvalidOperationException($"'{TagTypeName(arg.Tag)}' object cannot be interpreted as an integer");
            }

            return arg.IntegerValue;
        }

        static void RequireArity(string name, TaggedUnion[] args, int expected)
        {
            var actual = args == null ? 0 : args.Length;

            if (actual != expected)
            {
                throw new InvalidOperationException($"{name}() expected {expected} arguments, got {actual}");
            }
        }

        static bool IsNumeric(TaggedUnion val)
        {
            return val.Tag == Tag.Int || val.Tag == Tag.Float || val.Tag == Tag.Boolean;
        }

        static double AsDouble(TaggedUnion val)
        {
            switch (val.Tag)
            {
                case Tag.Int:
                {
                    return val.IntegerValue;
                }
                case Tag.Float:
                {
                    return val.FloatValue;
                }
                case Tag.Boolean:
                {
                    return val.BooleanValue ? 1.0 : 0.0;
                }
            }

            throw new InvalidOperationException("Value is not numeric");
        }

        static string FormatForPrint(TaggedUnion val)
        {
            switch (val.Tag)
            {
                case Tag.None:
                {
                    return "None";
                }
                case Tag.Boolean:
                {
                    return val.BooleanValue ? "True" : "False";
                }
                case Tag.Int:
                {
                    return val.IntegerValue.ToString(CultureInfo.InvariantCulture);
                }
                case Tag.Float:
                {
                    return val.FloatValue.ToString(CultureInfo.InvariantCulture);
                }
                case Tag.Str:
                {
                    return val.StringValue;
                }
                default:
                {
                    return val.GetTaggedValue()?.ToString() ?? string.Empty;
                }
            }
        }

        static string TagTypeName(Tag tag)
        {
            switch (tag)
            {
                case Tag.None:
                {
                    return "NoneType";
                }
                case Tag.Boolean:
                {
                    return "bool";
                }
                case Tag.Int:
                {
                    return "int";
                }
                case Tag.Float:
                {
                    return "float";
                }
                case Tag.Str:
                {
                    return "str";
                }
                case Tag.List:
                {
                    return "list";
                }
                case Tag.Dict:
                {
                    return "dict";
                }
                case Tag.Range:
                {
                    return "range";
                }
                default:
                {
                    return tag.ToString().ToLowerInvariant();
                }
            }
        }
    }
}
