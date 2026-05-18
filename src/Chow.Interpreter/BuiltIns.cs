using System;
using System.Collections.Generic;
using System.Globalization;
using Chow.Interpreter.Values;
using Chow.Interpreter.Values.DataTypes;
namespace Chow.Interpreter
{

    static class BuiltIns
    {
        static readonly Dictionary<BuiltInType, string> Names = new Dictionary<BuiltInType, string>
        {
            { BuiltInType.Print, "print" },
            { BuiltInType.Input, "input" },
            { BuiltInType.Clear, "clear" },
            { BuiltInType.Float, "float" },
            { BuiltInType.Str, "str" },
            { BuiltInType.Int, "int" },
            { BuiltInType.Bool, "bool" },
            { BuiltInType.List, "list" },
            { BuiltInType.Dict, "dict" },
            { BuiltInType.Len, "len" },
            { BuiltInType.Type, "type" },
            { BuiltInType.Abs, "abs" },
            { BuiltInType.Round, "round" },
            { BuiltInType.Min, "min" },
            { BuiltInType.Max, "max" },
            { BuiltInType.Range, "range" }
        };

        static readonly Dictionary<BuiltInType, Func<ChowValue[], ChowValue>> Defaults =
            new Dictionary<BuiltInType, Func<ChowValue[], ChowValue>>
            {
                { BuiltInType.Print, Print },
                { BuiltInType.Input, Input },
                { BuiltInType.Clear, Clear },
                { BuiltInType.Float, Float },
                { BuiltInType.Str, Str },
                { BuiltInType.Int, Int },
                { BuiltInType.Bool, Bool },
                { BuiltInType.List, List },
                { BuiltInType.Dict, Dict },
                { BuiltInType.Len, Len },
                { BuiltInType.Type, Type },
                { BuiltInType.Abs, Abs },
                { BuiltInType.Round, Round },
                { BuiltInType.Min, Min },
                { BuiltInType.Max, Max },
                { BuiltInType.Range, Range }
            };

        public static IEnumerable<BuiltInType> AllTypes => Names.Keys;

        public static string NameOf(BuiltInType type)
        {
            return Names[type];
        }

        public static Func<ChowValue[], ChowValue> DefaultOf(BuiltInType type)
        {
            return Defaults[type];
        }

        static ChowValue Print(ChowValue[] args)
        {
            RequireArity("print", args, 1);
            Console.WriteLine(FormatForPrint(args[0]));
            return ChowValue.None;
        }

        static ChowValue Input(ChowValue[] args)
        {
            var argCount = RequireArity("input", args, 0, 1);

            if (argCount == 1)
            {
                Console.Write(args[0]);
            }

            var input = Console.ReadLine();

            if (input == null)
            {
                input = string.Empty;
            }

            return new ChowValue(input);
        }

        static ChowValue Clear(ChowValue[] args)
        {
            RequireArity("clear", args, 0);

            Console.Clear();
            return ChowValue.None;
        }

        static ChowValue Float(ChowValue[] args)
        {
            RequireArity("float", args, 1);
            var val = args[0];

            switch (val.DataType)
            {
                case DataType.Float:
                {
                    return val;
                }
                case DataType.Int:
                {
                    return new ChowValue((double)val.AsType<long>());
                }
                case DataType.Bool:
                {
                    return new ChowValue(val.AsType<bool>() ? 1.0 : 0.0);
                }
                case DataType.Str:
                {
                    double parsed;

                    if (double.TryParse(val.AsType<string>(), NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                    {
                        return new ChowValue(parsed);
                    }

                    throw new InvalidOperationException($"could not convert string to float: '{val.AsType<string>()}'");
                }
            }

            throw new InvalidOperationException($"float() argument must be a string or a number, not '{DataTypeName(val.DataType)}'");
        }

        static ChowValue Str(ChowValue[] args)
        {
            RequireArity("str", args, 1);
            return new ChowValue(FormatForPrint(args[0]));
        }

        static ChowValue Int(ChowValue[] args)
        {
            RequireArity("int", args, 1);
            var val = args[0];

            switch (val.DataType)
            {
                case DataType.Int:
                {
                    return val;
                }
                case DataType.Float:
                {
                    return new ChowValue((long)val.AsType<double>());
                }
                case DataType.Bool:
                {
                    return new ChowValue(val.AsType<bool>() ? 1L : 0L);
                }
                case DataType.Str:
                {
                    long parsed;

                    if (long.TryParse(val.AsType<string>(), out parsed))
                    {
                        return new ChowValue(parsed);
                    }

                    throw new InvalidOperationException($"invalid literal for int() with base 10: '{val.AsType<string>()}'");
                }
            }

            throw new InvalidOperationException(
                $"int() argument must be a string, a bytes-like object or a real number, not '{DataTypeName(val.DataType)}'");
        }

        static ChowValue Bool(ChowValue[] args)
        {
            RequireArity("bool", args, 1);
            return new ChowValue(args[0].IsTruthy());
        }

        static ChowValue List(ChowValue[] args)
        {
            var argCount = args == null ? 0 : args.Length;

            if (argCount == 0)
            {
                return new ChowValue(new InternalList());
            }

            if (argCount == 1)
            {
                if (args[0].DataType == DataType.List)
                {
                    return new ChowValue(InternalList.Concat(args[0].AsType<InternalList>(), new InternalList()));
                }

                throw new InvalidOperationException($"'{DataTypeName(args[0].DataType)}' object is not iterable");
            }

            throw new InvalidOperationException($"list expected at most 1 argument, got {argCount}");
        }

        static ChowValue Dict(ChowValue[] args)
        {
            var argCount = args == null ? 0 : args.Length;

            if (argCount == 0)
            {
                return new ChowValue(new InternalDict());
            }

            if (argCount == 1)
            {
                if (args[0].DataType == DataType.Dict)
                {
                    return new ChowValue(InternalDict.Merge(args[0].AsType<InternalDict>(), new InternalDict()));
                }

                throw new InvalidOperationException($"'{DataTypeName(args[0].DataType)}' object is not iterable");
            }

            throw new InvalidOperationException($"dict expected at most 1 argument, got {argCount}");
        }

        static ChowValue Len(ChowValue[] args)
        {
            RequireArity("len", args, 1);
            var val = args[0];

            switch (val.DataType)
            {
                case DataType.Str:
                {
                    return new ChowValue(val.AsType<string>().Length);
                }
                case DataType.List:
                {
                    return new ChowValue(val.AsType<InternalList>().Count);
                }
                case DataType.Dict:
                {
                    return new ChowValue(val.AsType<InternalDict>().Count);
                }
            }

            throw new InvalidOperationException($"object of type '{DataTypeName(val.DataType)}' has no len()");
        }

        static ChowValue Type(ChowValue[] args)
        {
            RequireArity("type", args, 1);
            return new ChowValue(DataTypeName(args[0].DataType));
        }

        static ChowValue Abs(ChowValue[] args)
        {
            RequireArity("abs", args, 1);
            var val = args[0];

            switch (val.DataType)
            {
                case DataType.Int:
                {
                    return new ChowValue(Math.Abs(val.AsType<long>()));
                }
                case DataType.Float:
                {
                    return new ChowValue(Math.Abs(val.AsType<double>()));
                }
                case DataType.Bool:
                {
                    return new ChowValue(val.AsType<bool>() ? 1L : 0L);
                }
            }

            throw new InvalidOperationException($"bad operand type for abs(): '{DataTypeName(val.DataType)}'");
        }

        static ChowValue Round(ChowValue[] args)
        {
            RequireArity("round", args, 1);
            var val = args[0];

            switch (val.DataType)
            {
                case DataType.Int:
                {
                    return val;
                }
                case DataType.Float:
                {
                    return new ChowValue((long)Math.Round(val.AsType<double>(), MidpointRounding.ToEven));
                }
                case DataType.Bool:
                {
                    return new ChowValue(val.AsType<bool>() ? 1L : 0L);
                }
            }

            throw new InvalidOperationException($"type {DataTypeName(val.DataType)} doesn't define __round__ method");
        }

        static ChowValue Min(ChowValue[] args)
        {
            RequireArity("min", args, 2);

            if (!IsNumeric(args[0]) || !IsNumeric(args[1]))
            {
                throw new InvalidOperationException("min() arguments must be numbers");
            }

            return AsDouble(args[0]) <= AsDouble(args[1]) ? args[0] : args[1];
        }

        static ChowValue Max(ChowValue[] args)
        {
            RequireArity("max", args, 2);

            if (!IsNumeric(args[0]) || !IsNumeric(args[1]))
            {
                throw new InvalidOperationException("max() arguments must be numbers");
            }

            return AsDouble(args[0]) >= AsDouble(args[1]) ? args[0] : args[1];
        }

        static ChowValue Range(ChowValue[] args)
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

            return new ChowValue(new InternalRange(start, stop, step));
        }

        static long RequireRangeInt(ChowValue arg, int position)
        {
            if (arg.DataType != DataType.Int)
            {
                throw new InvalidOperationException($"'{DataTypeName(arg.DataType)}' object cannot be interpreted as an integer");
            }

            return arg.AsType<long>();
        }

        static void RequireArity(string name, ChowValue[] args, int expected)
        {
            var actual = args?.Length ?? 0;

            if (actual != expected)
            {
                throw new InvalidOperationException($"{name}() expected {expected} arguments, got {actual}");
            }
        }

        static int RequireArity(string name, ChowValue[] args, int minExpected, int maxExpected)
        {
            var actual = args?.Length ?? 0;

            if (actual < minExpected || actual > maxExpected)
            {
                throw new InvalidOperationException($"{name}() expected {minExpected} to {maxExpected} arguments, got {actual}");
            }

            return args?.Length ?? 0;
        }

        static bool IsNumeric(ChowValue val)
        {
            return val.DataType == DataType.Int || val.DataType == DataType.Float || val.DataType == DataType.Bool;
        }

        static double AsDouble(ChowValue val)
        {
            switch (val.DataType)
            {
                case DataType.Int:
                {
                    return val.AsType<long>();
                }
                case DataType.Float:
                {
                    return val.AsType<double>();
                }
                case DataType.Bool:
                {
                    return val.AsType<bool>() ? 1.0 : 0.0;
                }
            }

            throw new InvalidOperationException("Value is not numeric");
        }

        static string FormatForPrint(ChowValue val)
        {
            switch (val.DataType)
            {
                case DataType.None:
                {
                    return "None";
                }
                case DataType.Bool:
                {
                    return val.AsType<bool>() ? "True" : "False";
                }
                case DataType.Int:
                {
                    return val.AsType<long>().ToString(CultureInfo.InvariantCulture);
                }
                case DataType.Float:
                {
                    return val.AsType<double>().ToString(CultureInfo.InvariantCulture);
                }
                case DataType.Str:
                {
                    return val.AsType<string>();
                }
                default:
                {
                    return val.ToString() ?? string.Empty;
                }
            }
        }

        static string DataTypeName(DataType dataType)
        {
            switch (dataType)
            {
                case DataType.None:
                {
                    return "NoneType";
                }
                case DataType.Bool:
                {
                    return "bool";
                }
                case DataType.Int:
                {
                    return "int";
                }
                case DataType.Float:
                {
                    return "float";
                }
                case DataType.Str:
                {
                    return "str";
                }
                case DataType.List:
                {
                    return "list";
                }
                case DataType.Dict:
                {
                    return "dict";
                }
                case DataType.Range:
                {
                    return "range";
                }
                default:
                {
                    return dataType.ToString().ToLowerInvariant();
                }
            }
        }
    }
}
