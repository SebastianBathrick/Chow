using System;
using System.Collections.Generic;
using System.Globalization;
using Chow.Interpreter.State.Values;
using Chow.Interpreter.Values;
namespace Chow.Interpreter
{
    public static class BuiltIns
    {
        public static List<(string name, object funcObj)> GetFunctions()
        {
            return new List<(string name, object funcObj)>
            {
                ("print", (Func<ChowValue, ChowValue>)Print),
                ("input", (Func<ChowValue>)Input),
                ("float", (Func<ChowValue, ChowValue>)Float),
                ("str", (Func<ChowValue, ChowValue>)Str),
                ("int", (Func<ChowValue, ChowValue>)Int),
                ("bool", (Func<ChowValue, ChowValue>)Bool),
                ("list", (Func<TaggedUnion[], TaggedUnion>)List),
                ("dict", (Func<TaggedUnion[], TaggedUnion>)Dict),
                ("len", (Func<ChowValue, ChowValue>)Len),
                ("abs", (Func<ChowValue, ChowValue>)Abs),
                ("round", (Func<ChowValue, ChowValue>)Round),
                ("min", (Func<TaggedUnion[], TaggedUnion>)Min),
                ("max", (Func<TaggedUnion[], TaggedUnion>)Max),
            };
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

            throw new InvalidOperationException(
                $"int() argument must be a string, a bytes-like object or a real number, not '{ChowTypeName(val)}'");
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
