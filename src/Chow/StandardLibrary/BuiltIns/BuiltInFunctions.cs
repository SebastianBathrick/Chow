using System;
using System.Collections.Generic;
using Chow.Objects;
using Chow.VM;
using Chow.VM.Utilities;
namespace Chow.StandardLibrary.BuiltIns
{
    static class BuiltInFunctions
    {
        // Leaving callables as objects because SourceValue will box it into one anyway, and to make
        // it easy to change built-in function object types later. The way built-in functions are
        // currently handled is likely a temporary solution and is subject to change.
        static List<(string name, object callableObject)> _namedInvocableObjects;

        #region Properties

        /// <summary>List of built-in function names paired with their first-class function objects.</summary>
        public static List<(string name, object callableObject)> NamedInvocableObjects
        {
            get
            {
                if (_namedInvocableObjects != null)
                {
                    return _namedInvocableObjects;
                }
                
                // Lazily initialize in case the library client never imports built-ins
                _namedInvocableObjects = CreateBuiltInsMap();
                
                // Return a copy to avoid accidentally mutating the built-in functions field
                return new List<(string name, object callableObject)>(_namedInvocableObjects);
            }
        }

        // This property is used for unit tests
        public static string[] InvocableObjectNames
        {
            get
            {
                if (_namedInvocableObjects == null)
                {
                    _namedInvocableObjects = CreateBuiltInsMap();
                }
                
                var names = new string[_namedInvocableObjects.Count];

                for (var i = 0; i < _namedInvocableObjects.Count; i++)
                {
                    names[i] = _namedInvocableObjects[i].name;
                }

                return names;
            }
        }

        #endregion

        #region Invocable Object Creation Methods

        static List<(string, object)> CreateBuiltInsMap()
        {
            var printDef = new BuiltInDefinition(BUILT_IN_NAME_PRINT, Print, 0, short.MaxValue);
            var inputDef = new BuiltInDefinition(BUILT_IN_NAME_INPUT, Input, 0, 1);
            var clearDef = new BuiltInDefinition(BUILT_IN_NAME_CLEAR, Clear);
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

            return new List<(string, object)>
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
        
        static Func<SourceValue[], SourceValue> BuildGuardedBuiltIn(BuiltInDefinition builtInDef)
        {
            SourceValue GuardedDelegateInvocation(SourceValue[] args)
            {
                if (builtInDef.HasParameters)
                {
                    ValidateArgumentCount(builtInDef, args);

                    if (!builtInDef.IsVoid)
                    {
                        return builtInDef.ValueReturnDelegateWithParams(args);
                    }

                    builtInDef.VoidDelegateWithParams(args);
                    return SourceValue.None;
                }

                if (!builtInDef.IsVoid)
                {
                    return builtInDef.ValueReturnDelegate();
                }

                builtInDef.VoidDelegate();
                return SourceValue.None;
            }

            return GuardedDelegateInvocation;
        }
        
        static void ValidateArgumentCount(BuiltInDefinition builtInDef, SourceValue[] args)
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

            // Python raises TypeError for wrong arg count to built-ins.
            throw new DataTypeException(
                $"{builtInDef.Name}() takes {builtInDef.MinimumArguments} to {builtInDef.MaximumArguments} arguments");
        }

        #endregion

        #region Invocable Object Methods

        // NOTE: When converting ChowValues to other types, use the internal conversion methods
        // like SourceValue.ToBool(), SourceValue.ToSource(), SourceValue.ToLong(), etc...

        static void Print(SourceValue[] args)
        {
            // Match Python: arguments are space-separated and the line is terminated with a single
            // newline. Zero-arg call still emits a blank line.
            if (HasZeroArguments(args))
            {
                Console.WriteLine();
                return;
            }

            for (var i = 0; i < args.Length; i++)
            {
                if (i > 0)
                {
                    Console.Write(' ');
                }

                Console.Write(args[i].ToString());
            }

            Console.WriteLine();
        }



        static SourceValue Input(SourceValue[] args)
        {
            // If there is an argument, then that is considered a prompt
            if (!HasZeroArguments(args))
            {
                // The prompt will not end the line
                Console.Write(args[0].ToString());
            }

            var input = Console.ReadLine() ?? string.Empty;
            return new SourceValue(input);
        }

        static void Clear()
        {
            Console.Clear();
        }

        static SourceValue Float(SourceValue[] args)
        {
            if (HasZeroArguments(args))
            {
                return new SourceValue(0.0);
            }

            return new SourceValue(args[0].ToDouble());
        }

        static SourceValue Str(SourceValue[] args)
        {
            if (HasZeroArguments(args))
            {
                return new SourceValue(string.Empty);
            }

            return new SourceValue(args[0].ToString());
        }

        static SourceValue Int(SourceValue[] args)
        {
            if (HasZeroArguments(args))
            {
                return new SourceValue(0L);
            }

            return new SourceValue(args[0].ToLong());
        }

        static SourceValue Bool(SourceValue[] args)
        {
            if (HasZeroArguments(args))
            {
                return new SourceValue(false);
            }

            return new SourceValue(args[0].ToBool());
        }

        static SourceValue List(SourceValue[] args)
        {
            var result = new SourceList();

            if (!HasZeroArguments(args))
            {
                var iterator = IteratorFactory.GetIterator(args[0]);
                while (iterator.TryMoveNext(out var current))
                {
                    result.Add(current);
                }
            }

            return new SourceValue(result);
        }

        static SourceValue Dict(SourceValue[] args)
        {
            var result = new SourceDictionary();

            if (!HasZeroArguments(args))
            {
                if (args[0].DataType != DataType.Dict)
                {
                    // Python: TypeError. Chow has no kwargs/mapping protocol yet, so only dict copy is supported.
                    throw new DataTypeException($"'{args[0].DataType}' object is not iterable");
                }

                result.GetMethod("update")(new[]
                {
                    args[0],
                });
            }

            return new SourceValue(result);
        }

        static SourceValue Len(SourceValue[] args)
        {
            var value = args[0];

            switch (value.DataType)
            {
                case DataType.Str:
                    return new SourceValue(value.ToString().Length);
                case DataType.List:
                    return new SourceValue(((SourceList)value.ToObject()).Count);
                case DataType.Dict:
                    return new SourceValue(((SourceDictionary)value.ToObject()).Count);
                case DataType.Range:
                    return new SourceValue(((SourceRange)value.ToObject()).Count);
            }

            throw new DataTypeException($"object of type '{value.DataType}' has no len()");
        }

        static SourceValue Abs(SourceValue[] args)
        {
            var value = args[0];

            switch (value.DataType)
            {
                case DataType.Long:
                    return new SourceValue(Math.Abs(value.ToLong()));
                case DataType.Double:
                    return new SourceValue(Math.Abs(value.ToDouble()));
                case DataType.Bool:
                    return new SourceValue(value.ToBool() ? 1L : 0L);
            }

            throw new DataTypeException($"bad operand type for abs(): '{value.DataType}'");
        }

        static SourceValue Round(SourceValue[] args)
        {
            var number = args[0].ToDouble();

            if (args.Length == 1)
            {
                return new SourceValue((long)Math.Round(number, MidpointRounding.ToEven));
            }

            var ndigits = (int)args[1].ToLong();
            return new SourceValue(Math.Round(number, ndigits, MidpointRounding.ToEven));
        }

        static SourceValue Min(SourceValue[] args)
        {
            return MinMax(args, true, "min");
        }

        static SourceValue Max(SourceValue[] args)
        {
            return MinMax(args, false, "max");
        }

        static SourceValue MinMax(SourceValue[] args, bool findLess, string name)
        {
            IIterator iterator;

            if (args.Length == 1)
            {
                iterator = IteratorFactory.GetIterator(args[0]);
            }
            else
            {
                var packed = new SourceList();

                foreach (var arg in args)
                {
                    packed.Add(arg);
                }

                iterator = new SourceListIterator(packed);
            }

            if (!iterator.TryMoveNext(out var winner))
            {
                // Python: ValueError. No ValueException type in Chow yet.
                throw new InvalidOperationException($"{name}() arg is an empty sequence");
            }

            while (iterator.TryMoveNext(out var next))
            {
                var replace = findLess
                    ? ComparisonEvaluator.EvaluateLess(r: winner, l: next).ToBool()
                    : ComparisonEvaluator.EvaluateGreater(r: winner, l: next).ToBool();
                
                if (replace)
                {
                    winner = next;
                }
            }

            return winner;
        }

        static SourceValue Range(SourceValue[] args)
        {
            long start;
            long stop;
            long step;

            if (args.Length == 1)
            {
                start = 0;
                stop = RequireRangeInt(args[0]);
                step = 1;
            }
            else if (args.Length == 2)
            {
                start = RequireRangeInt(args[0]);
                stop = RequireRangeInt(args[1]);
                step = 1;
            }
            else
            {
                start = RequireRangeInt(args[0]);
                stop = RequireRangeInt(args[1]);
                step = RequireRangeInt(args[2]);

                if (step == 0)
                {
                    // Python: ValueError. No ValueException type in Chow yet.
                    throw new InvalidOperationException("range() arg 3 must not be zero");
                }
            }

            return new SourceValue(new SourceRange(start, stop, step));
        }

        static long RequireRangeInt(SourceValue arg)
        {
            if (arg.DataType != DataType.Long)
            {
                throw new DataTypeException(
                    $"'{arg.DataType}' object cannot be interpreted as an integer");
            }

            return arg.AsType<long>();
        }

        static bool HasZeroArguments(SourceValue[] args)
        {
            return args == null || args.Length == 0;
        }

        #endregion

        #region Constants
        
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
