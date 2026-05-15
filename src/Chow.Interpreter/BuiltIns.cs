using Chow.Interpreter.State.Values;
using Chow.Interpreter.Values;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Chow.Interpreter
{
    /// <summary>
    /// Stable identifiers for the standard built-in functions. Hosts use these with
    /// <see cref="ChowModule.SetBuiltInActive"/>, <see cref="ChowModule.SetBuiltInValue"/>, and
    /// <see cref="ChowModule.IsBuiltInActive"/> to sandbox or override individual built-ins.
    /// </summary>
    public enum BuiltInType
    {
        /// <summary><c>print(value)</c> — writes <c>value</c> to standard output and returns <c>None</c>.</summary>
        Print,
        /// <summary><c>input()</c> — reads a line from standard input and returns it as a string.</summary>
        Input,
        /// <summary><c>float(value)</c> — converts a number or numeric string to a float.</summary>
        Float,
        /// <summary><c>str(value)</c> — converts any value to its string representation.</summary>
        Str,
        /// <summary><c>int(value)</c> — converts a number, bool, or numeric string to an int (truncating floats).</summary>
        Int,
        /// <summary><c>bool(value)</c> — converts a value to a bool using Python truthiness rules.</summary>
        Bool,
        /// <summary><c>list()</c> / <c>list(iterable)</c> — constructs a list (empty or a copy of the argument).</summary>
        List,
        /// <summary><c>dict()</c> / <c>dict(mapping)</c> — constructs a dict (empty or a copy of the argument).</summary>
        Dict,
        /// <summary><c>len(value)</c> — returns the length of a string, list, or dict.</summary>
        Len,
        /// <summary><c>type(value)</c> — returns the Python-style type name as a string.</summary>
        Type,
        /// <summary><c>abs(value)</c> — returns the absolute value of a number.</summary>
        Abs,
        /// <summary><c>round(value)</c> — rounds to the nearest integer using banker's rounding.</summary>
        Round,
        /// <summary><c>min(a, b)</c> — returns the smaller of two numbers.</summary>
        Min,
        /// <summary><c>max(a, b)</c> — returns the larger of two numbers.</summary>
        Max,
        /// <summary><c>range(stop)</c> / <c>range(start, stop)</c> / <c>range(start, stop, step)</c> — produces a lazy integer sequence.</summary>
        Range,
    }

    /// <summary>
    /// Internal source-of-truth table for the standard built-ins: maps each <see cref="BuiltInType"/> to its
    /// source-language name and default implementation. Hosts do not interact with this class directly —
    /// the table is consumed by <see cref="ChowModule"/> at construction to seed the module's global scope.
    /// </summary>
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

        static readonly Dictionary<BuiltInType, object> _defaults = new Dictionary<BuiltInType, object>
        {
            { BuiltInType.Print, (Func<ChowValue, ChowValue>)Print          },
            { BuiltInType.Input, (Func<ChowValue>)Input                     },
            { BuiltInType.Float, (Func<ChowValue, ChowValue>)Float          },
            { BuiltInType.Str,   (Func<ChowValue, ChowValue>)Str            },
            { BuiltInType.Int,   (Func<ChowValue, ChowValue>)Int            },
            { BuiltInType.Bool,  (Func<ChowValue, ChowValue>)Bool           },
            { BuiltInType.List,  (Func<TaggedUnion[], TaggedUnion>)List     },
            { BuiltInType.Dict,  (Func<TaggedUnion[], TaggedUnion>)Dict     },
            { BuiltInType.Len,   (Func<ChowValue, ChowValue>)Len            },
            { BuiltInType.Type,  (Func<ChowValue, ChowValue>)Type           },
            { BuiltInType.Abs,   (Func<ChowValue, ChowValue>)Abs            },
            { BuiltInType.Round, (Func<ChowValue, ChowValue>)Round          },
            { BuiltInType.Min,   (Func<TaggedUnion[], TaggedUnion>)Min      },
            { BuiltInType.Max,   (Func<TaggedUnion[], TaggedUnion>)Max      },
            { BuiltInType.Range, (Func<TaggedUnion[], TaggedUnion>)Range    },
        };

        public static IEnumerable<BuiltInType> AllTypes => _names.Keys;

        public static string NameOf(BuiltInType type)
        {
            return _names[type];
        }

        public static object DefaultOf(BuiltInType type)
        {
            return _defaults[type];
        }

        static ChowValue Print(ChowValue val)
        {
            Console.WriteLine(val);
            return ChowValue.None;
        }

        static ChowValue Input()
        {
            var input = Console.ReadLine();

            if (input == null)
            {
                input = string.Empty;
            }

            return new ChowStr(input);
        }

        static ChowValue Float(ChowValue val)
        {
            if (val.IsType<double>())
            {
                return new ChowFloat(val.AsType<double>());
            }

            if (val.IsType<long>())
            {
                return new ChowFloat(val.AsType<long>());
            }

            if (val.IsType<bool>())
            {
                return new ChowFloat(val.AsType<double>());
            }

            if (val is ChowStr str)
            {
                double parsed;
                if (double.TryParse(str.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                {
                    return new ChowFloat(parsed);
                }

                throw new InvalidOperationException($"could not convert string to float: '{str.Value}'");
            }

            throw new InvalidOperationException($"float() argument must be a string or a number, not '{ChowTypeName(val)}'");
        }

        static ChowValue Str(ChowValue val)
        {
            return new ChowStr(val.ToString());
        }

        static ChowValue Int(ChowValue val)
        {
            if (val.IsType<long>())
            {
                return new ChowInt(val.AsType<long>());
            }

            if (val.IsType<double>())
            {
                return new ChowInt((long)val.AsType<double>());
            }

            if (val.IsType<bool>())
            {
                return new ChowInt(val.AsType<long>());
            }

            if (val is ChowStr str)
            {
                long parsed;
                if (long.TryParse(str.Value, out parsed))
                {
                    return new ChowInt(parsed);
                }

                throw new InvalidOperationException($"invalid literal for int() with base 10: '{str.Value}'");
            }

            throw new InvalidOperationException($"int() argument must be a string, a bytes-like object or a real number, not '{ChowTypeName(val)}'");
        }

        static ChowValue Bool(ChowValue val)
        {
            if (val.IsNone)
            {
                return new ChowBool(false);
            }

            if (val.IsType<bool>())
            {
                return new ChowBool(val.AsType<bool>());
            }

            if (val.IsType<long>())
            {
                return new ChowBool(val.AsType<long>() != 0);
            }

            if (val.IsType<double>())
            {
                return new ChowBool(val.AsType<double>() != 0.0);
            }

            if (val is ChowStr str)
            {
                return new ChowBool(str.Value.Length != 0);
            }

            if (val is ChowList list)
            {
                return new ChowBool(list.Count != 0);
            }

            if (val is ChowDict dict)
            {
                return new ChowBool(dict.Count != 0);
            }

            throw new InvalidOperationException($"bool() argument not supported for type '{ChowTypeName(val)}'");
        }

        static TaggedUnion List(TaggedUnion[] args)
        {
            return ApiConverter.ToTaggedUnion(List(ToChowValues(args)));
        }

        static ChowValue List(ChowValue[] args)
        {
            if (args.Length == 0)
            {
                return new ChowList();
            }

            if (args.Length == 1)
            {
                if (args[0] is ChowList list)
                {
                    return new ChowList(list);
                }

                throw new InvalidOperationException($"'{ChowTypeName(args[0])}' object is not iterable");
            }

            throw new InvalidOperationException($"list expected at most 1 argument, got {args.Length}");
        }

        static TaggedUnion Dict(TaggedUnion[] args)
        {
            return ApiConverter.ToTaggedUnion(Dict(ToChowValues(args)));
        }

        static ChowValue Dict(ChowValue[] args)
        {
            if (args.Length == 0)
            {
                return new ChowDict();
            }

            if (args.Length == 1)
            {
                if (args[0] is ChowDict dict)
                {
                    return new ChowDict(dict);
                }

                throw new InvalidOperationException($"'{ChowTypeName(args[0])}' object is not iterable");
            }

            throw new InvalidOperationException($"dict expected at most 1 argument, got {args.Length}");
        }

        static ChowValue Len(ChowValue val)
        {
            if (val is ChowStr str)
            {
                return new ChowInt(str.Value.Length);
            }

            if (val is ChowList list)
            {
                return new ChowInt(list.Count);
            }

            if (val is ChowDict dict)
            {
                return new ChowInt(dict.Count);
            }

            throw new InvalidOperationException($"object of type '{ChowTypeName(val)}' has no len()");
        }

        static ChowValue Type(ChowValue val)
        {
            return new ChowStr(ChowTypeName(val));
        }

        static ChowValue Abs(ChowValue val)
        {
            if (val.IsType<long>())
            {
                return new ChowInt(Math.Abs(val.AsType<long>()));
            }

            if (val.IsType<double>())
            {
                return new ChowFloat(Math.Abs(val.AsType<double>()));
            }

            if (val.IsType<bool>())
            {
                return new ChowInt(val.AsType<long>());
            }

            throw new InvalidOperationException($"bad operand type for abs(): '{ChowTypeName(val)}'");
        }

        static ChowValue Round(ChowValue val)
        {
            if (val.IsType<long>())
            {
                return new ChowInt(val.AsType<long>());
            }

            if (val.IsType<double>())
            {
                return new ChowInt((long)Math.Round(val.AsType<double>(), MidpointRounding.ToEven));
            }

            if (val.IsType<bool>())
            {
                return new ChowInt(val.AsType<long>());
            }

            throw new InvalidOperationException($"type {ChowTypeName(val)} doesn't define __round__ method");
        }

        static TaggedUnion Min(TaggedUnion[] args)
        {
            return ApiConverter.ToTaggedUnion(Min(ToChowValues(args)));
        }

        static ChowValue Min(ChowValue[] args)
        {
            if (args.Length != 2)
            {
                throw new InvalidOperationException($"min() expected 2 arguments, got {args.Length}");
            }

            if (!ChowIsNumeric(args[0]) || !ChowIsNumeric(args[1]))
            {
                throw new InvalidOperationException("min() arguments must be numbers");
            }

            return ChowAsDouble(args[0]) <= ChowAsDouble(args[1]) ? args[0] : args[1];
        }

        static TaggedUnion Max(TaggedUnion[] args)
        {
            return ApiConverter.ToTaggedUnion(Max(ToChowValues(args)));
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
            // Python's range only accepts integers, not floats. Match that behavior.
            if (arg.Tag != Tag.Int)
            {
                throw new InvalidOperationException($"'{TagToTypeName(arg.Tag)}' object cannot be interpreted as an integer");
            }

            return arg.IntegerValue;
        }

        static string TagToTypeName(Tag tag)
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

        static ChowValue Max(ChowValue[] args)
        {
            if (args.Length != 2)
            {
                throw new InvalidOperationException($"max() expected 2 arguments, got {args.Length}");
            }

            if (!ChowIsNumeric(args[0]) || !ChowIsNumeric(args[1]))
            {
                throw new InvalidOperationException("max() arguments must be numbers");
            }

            return ChowAsDouble(args[0]) >= ChowAsDouble(args[1]) ? args[0] : args[1];
        }

        static ChowValue[] ToChowValues(TaggedUnion[] args)
        {
            if (args == null)
            {
                return new ChowValue[0];
            }

            var result = new ChowValue[args.Length];
            for (var i = 0; i < args.Length; i++)
            {
                result[i] = ApiConverter.ToChowValue(args[i]);
            }

            return result;
        }

        static string ChowTypeName(ChowValue val)
        {
            // TODO: Centralize type names, so that they're not hardcoded in multiple places
            if (val.IsNone)
            {
                return "NoneType";
            }

            if (val.IsType<bool>())
            {
                return "bool";
            }

            if (val.IsType<long>())
            {
                return "int";
            }

            if (val.IsType<double>())
            {
                return "float";
            }

            if (val is ChowStr)
            {
                return "str";
            }

            if (val is ChowList)
            {
                return "list";
            }

            if (val is ChowDict)
            {
                return "dict";
            }

            if (val is ChowRange)
            {
                return "range";
            }

            if (val is ChowDynamic dynamic && dynamic.Value != null)
            {
                return dynamic.Value.GetType().Name;
            }

            return "object";
        }

        static bool ChowIsNumeric(ChowValue val)
        {
            return val.IsType<long>() || val.IsType<double>() || val.IsType<bool>();
        }

        static double ChowAsDouble(ChowValue val)
        {
            if (val.IsType<long>())
            {
                return val.AsType<long>();
            }

            if (val.IsType<double>())
            {
                return val.AsType<double>();
            }

            if (val.IsType<bool>())
            {
                return val.AsType<long>();
            }

            throw new InvalidOperationException("Value is not numeric");
        }
    }
}
