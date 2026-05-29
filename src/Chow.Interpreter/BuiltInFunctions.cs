using System;
using System.Collections.Generic;
using Chow.Interpreter.DataTypes;
using Chow.Interpreter.Exceptions;
namespace Chow.Interpreter
{
    public static class BuiltInFunctions
    {
        // Will leave null in case built-in functions are never used
        static List<(string name, Func<ChowValue[], ChowValue> interopFunc)> _builtInFunctions = null;

        static Func<ChowValue[], ChowValue> BuildGuardedBuiltIn(BuiltInDefinition builtInDef)
        {
            ChowValue GuardedDelegateInvocation(ChowValue[] args)
            {
                if (builtInDef.HasParameters)
                {
                    ValidateArgumentCount(builtInDef, args);

                    if (!builtInDef.IsVoid)
                    {
                        return builtInDef.ValueReturnDelegateWithParams(args);
                    }

                    builtInDef.VoidDelegateWithParams(args);
                    return ChowValue.None;
                }

                if (!builtInDef.IsVoid)
                {
                    return builtInDef.ValueReturnDelegate();
                }

                builtInDef.VoidDelegate();
                return ChowValue.None;
            }

            return GuardedDelegateInvocation;
        }

        static List<(string, Func<ChowValue[], ChowValue>)> CreateBuiltInsMap()
        {
            var printDef = new BuiltInDefinition(BUILT_IN_NAME_PRINT, Print, 0, short.MaxValue);
            var inputDef = new BuiltInDefinition(BUILT_IN_NAME_INPUT, Input, 0, 1);
            var clearDef = new BuiltInDefinition(BUILT_IN_NAME_CLEAR, Clear, 0, 0);
            var floatDef = new BuiltInDefinition(BUILT_IN_NAME_FLOAT, Float, 0, 1);
            var strDef = new BuiltInDefinition(BUILT_IN_NAME_STR, Str, 0, 1);
            var intDef = new BuiltInDefinition(BUILT_IN_NAME_INT, Int, 0, 1);
            var boolDef = new BuiltInDefinition(BUILT_IN_NAME_BOOL, Bool, 0, 1);
            var listDef = new BuiltInDefinition(BUILT_IN_NAME_LIST, List, 0, 1);
            var dictDef = new BuiltInDefinition(BUILT_IN_NAME_DICT, Dict, 0, 1);
            var lenDef = new BuiltInDefinition(BUILT_IN_NAME_LEN, Len, 1, 1);
            var absDef = new BuiltInDefinition(BUILT_IN_NAME_ABS, Abs, 1, 1);
            var roundDef = new BuiltInDefinition(BUILT_IN_NAME_ROUND, Round, 1, 2);
            var minDef = new BuiltInDefinition(BUILT_IN_NAME_MIN, Min, 1, short.MaxValue);
            var maxDef = new BuiltInDefinition(BUILT_IN_NAME_MAX, Max, 1, short.MaxValue);
            var rangeDef = new BuiltInDefinition(BUILT_IN_NAME_RANGE, Range, 1, 3);

            return new List<(string, Func<ChowValue[], ChowValue>)>
            {
                (printDef.Name, BuildGuardedBuiltIn(printDef)),
                (inputDef.Name, BuildGuardedBuiltIn(inputDef)),
                (clearDef.Name, BuildGuardedBuiltIn(clearDef)),
                (floatDef.Name, BuildGuardedBuiltIn(floatDef)),
                (strDef.Name, BuildGuardedBuiltIn(strDef)),
                (intDef.Name, BuildGuardedBuiltIn(intDef)),
                (boolDef.Name, BuildGuardedBuiltIn(boolDef)),
                (listDef.Name, BuildGuardedBuiltIn(listDef)),
                (dictDef.Name, BuildGuardedBuiltIn(dictDef)),
                (lenDef.Name, BuildGuardedBuiltIn(lenDef)),
                (absDef.Name, BuildGuardedBuiltIn(absDef)),
                (roundDef.Name, BuildGuardedBuiltIn(roundDef)),
                (minDef.Name, BuildGuardedBuiltIn(minDef)),
                (maxDef.Name, BuildGuardedBuiltIn(maxDef)),
                (rangeDef.Name, BuildGuardedBuiltIn(rangeDef)),
            };
        }
        
        /*
        public static ChowModule BuildModule()
        {
            if (_builtInFunctions == null)
            {
                _builtInFunctions = CreateBuiltInsMap();
            }
            
            // Always make a new instance because the client can reassign a module's global values
            // (i.e., reassign the variables that store the built-in function object(s)). Creating
            // a new module each call means the client never loses the default behaviors of the
            // built-in functions
            var builtInsModule = new ChowModule(BUILT_INS_MODULE_NAME);

            foreach (var builtIn in _builtInFunctions)
            {
                builtInsModule[builtIn.name] = builtIn.interopFunc;
            }

            return builtInsModule;
        }
        */

        static void ValidateArgumentCount(BuiltInDefinition builtInDef, ChowValue[] args)
        {
            if (args != null)
            {
                if (args.Length >= builtInDef.MinimumArguments && args.Length <= builtInDef.MaximumArguments)
                {
                    return;
                }
            }
            else if (builtInDef.MinimumArguments == 0)
            {
                return;
            }

            throw new ArgumentException(
                $"{builtInDef.Name} must be called with {builtInDef.MinimumArguments} to {builtInDef.MaximumArguments}.");
        }

        #region Methods for Delegates

        // NOTE: When converting ChowValues to other types, use the internal conversion methods
        // like ChowValue.ToBool(), ChowValue.ToStr(), ChowValue.ToInt64(), etc...

        static void Print(ChowValue[] args)
        {
            // Print a blank line if there are no arguments
            if (HasZeroArguments(args))
            {
                Console.WriteLine();
                return;
            }

            foreach (var arg in args)
            {
                Console.WriteLine(arg.ToStr());
            }
        }



        static ChowValue Input(ChowValue[] args)
        {
            // If there is an argument, then that is considered a prompt
            if (!HasZeroArguments(args))
            {
                // The prompt will not end the line
                Console.Write(args[0].ToStr());
            }

            var input = Console.ReadLine() ?? string.Empty;
            return new ChowValue(input);
        }

        static void Clear(ChowValue[] args)
        {
            Console.Clear();
        }

        static ChowValue Float(ChowValue[] args)
        {
            if (HasZeroArguments(args))
            {
                return new ChowValue(0.0);
            }

            return new ChowValue(args[0].ToFloat64());
        }

        static ChowValue Str(ChowValue[] args)
        {
            if (HasZeroArguments(args))
            {
                return new ChowValue(string.Empty);
            }

            return new ChowValue(args[0].ToStr());
        }

        static ChowValue Int(ChowValue[] args)
        {
            if (HasZeroArguments(args))
            {
                return new ChowValue(0L);
            }

            return new ChowValue(args[0].ToInt64());
        }

        static ChowValue Bool(ChowValue[] args)
        {
            if (HasZeroArguments(args))
            {
                return new ChowValue(false);
            }

            return new ChowValue(args[0].ToBool());
        }

        static ChowValue List(ChowValue[] args)
        {
            var result = new InternalList();

            if (!HasZeroArguments(args))
            {
                var iterator = IteratorFactory.GetIterator(args[0]);
                while (iterator.TryMoveNext(out var current))
                {
                    result.Add(current);
                }
            }

            return new ChowValue(result);
        }

        static ChowValue Dict(ChowValue[] args)
        {
            var result = new InternalDict();

            if (!HasZeroArguments(args))
            {
                if (args[0].DataType != DataType.Dict)
                {
                    // Python: TypeError. Chow has no kwargs/mapping protocol yet, so only dict copy is supported.
                    throw new TypeException($"'{args[0].DataType}' object is not iterable");
                }

                result.GetMethod("update")(new[]
                {
                    args[0],
                });
            }

            return new ChowValue(result);
        }

        static ChowValue Len(ChowValue[] args)
        {
            var value = args[0];

            switch (value.DataType)
            {
                case DataType.Str:
                    {
                        return new ChowValue(value.AsType<string>().Length);
                    }
                case DataType.List:
                    {
                        return new ChowValue(value.AsType<InternalList>().Count);
                    }
                case DataType.Dict:
                    {
                        return new ChowValue(value.AsType<InternalDict>().Count);
                    }
                case DataType.Range:
                    {
                        return new ChowValue(value.AsType<InternalRange>().Count);
                    }
            }

            throw new TypeException($"object of type '{value.DataType}' has no len()");
        }

        static ChowValue Abs(ChowValue[] args)
        {
            var value = args[0];

            switch (value.DataType)
            {
                case DataType.Int:
                    {
                        return new ChowValue(Math.Abs(value.AsType<long>()));
                    }
                case DataType.Float:
                    {
                        return new ChowValue(Math.Abs(value.AsType<double>()));
                    }
                case DataType.Bool:
                    {
                        return new ChowValue(value.AsType<bool>() ? 1L : 0L);
                    }
            }

            throw new TypeException($"bad operand type for abs(): '{value.DataType}'");
        }

        static ChowValue Round(ChowValue[] args)
        {
            var number = args[0].ToFloat64();

            if (args.Length == 1)
            {
                return new ChowValue((long)Math.Round(number, MidpointRounding.ToEven));
            }

            var ndigits = (int)args[1].ToInt64();
            return new ChowValue(Math.Round(number, ndigits, MidpointRounding.ToEven));
        }

        static ChowValue Min(ChowValue[] args)
        {
            return MinMax(args, true, "min");
        }

        static ChowValue Max(ChowValue[] args)
        {
            return MinMax(args, false, "max");
        }

        static ChowValue MinMax(ChowValue[] args, bool findLess, string name)
        {
            IChowIterator iterator;

            if (args.Length == 1)
            {
                iterator = IteratorFactory.GetIterator(args[0]);
            }
            else
            {
                var packed = new InternalList();

                foreach (var arg in args)
                {
                    packed.Add(arg);
                }

                iterator = new InternalListIterator(packed);
            }

            if (!iterator.TryMoveNext(out var winner))
            {
                // Python: ValueError. No ValueException type in Chow yet.
                throw new InvalidOperationException($"{name}() arg is an empty sequence");
            }

            while (iterator.TryMoveNext(out var next))
            {
                var replace = findLess ? next.IsLessThan(winner) : next.IsGreaterThan(winner);
                
                if (replace)
                {
                    winner = next;
                }
            }

            return winner;
        }

        static ChowValue Range(ChowValue[] args)
        {
            long start;
            long stop;
            long step;

            if (args.Length == 1)
            {
                start = 0;
                stop = RequireRangeInt(args[0], 0);
                step = 1;
            }
            else if (args.Length == 2)
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
                    // Python: ValueError. No ValueException type in Chow yet.
                    throw new InvalidOperationException("range() arg 3 must not be zero");
                }
            }

            return new ChowValue(new InternalRange(start, stop, step));
        }

        static long RequireRangeInt(ChowValue arg, int position)
        {
            if (arg.DataType != DataType.Int)
            {
                throw new TypeException(
                    $"'{arg.DataType}' object cannot be interpreted as an integer");
            }

            return arg.AsType<long>();
        }

        static bool HasZeroArguments(ChowValue[] args)
        {
            return args == null || args.Length == 0;
        }

        #endregion

        #region Constants

        const string BUILT_INS_MODULE_NAME = "builtins";
        const string BUILT_IN_NAME_PRINT = "print";
        const string BUILT_IN_NAME_INPUT = "input";
        const string BUILT_IN_NAME_CLEAR = "clear";
        const string BUILT_IN_NAME_FLOAT = "float";
        const string BUILT_IN_NAME_STR = "str";
        const string BUILT_IN_NAME_INT = "int";
        const string BUILT_IN_NAME_BOOL = "bool";
        const string BUILT_IN_NAME_LIST = "list";
        const string BUILT_IN_NAME_DICT = "dict";
        const string BUILT_IN_NAME_LEN = "len";
        const string BUILT_IN_NAME_ABS = "abs";
        const string BUILT_IN_NAME_ROUND = "round";
        const string BUILT_IN_NAME_MIN = "min";
        const string BUILT_IN_NAME_MAX = "max";
        const string BUILT_IN_NAME_RANGE = "range";

        #endregion
    }
}
