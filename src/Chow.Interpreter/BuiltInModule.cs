using System;
using System.Collections.Generic;
using Chow.Interpreter.Values;

namespace Chow.Interpreter
{
    public static class BuiltInModule
    {

        static readonly List<BuiltInDefinition> _functions = new List<BuiltInDefinition>()
        {

        };

        static Func<ChowValue[], ChowValue> BuildGuardedBuiltInFunction(BuiltInDefinition builtInDef)
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
        
        static void ValidateArgumentCount(BuiltInDefinition builtInDef, ChowValue[] args)
        {
            if (args.Length >= builtInDef.MinimumArguments && args.Length <= builtInDef.MaximumArguments)
            {
                return;
            }

            throw new ArgumentException(
                $"{builtInDef.Name} must be called with {builtInDef.MinimumArguments } to {builtInDef.MaximumArguments}.");
        }
    }

    readonly struct BuiltInDefinition
    {
        const int ARGUMENT_COUNT_UNDEFINED = 0;
        
        public string Name { get; }
        public int MinimumArguments { get; }
        public int MaximumArguments { get; }

        public bool HasParameters => MaximumArguments > ARGUMENT_COUNT_UNDEFINED;
        
        public Func<ChowValue[], ChowValue>  ValueReturnDelegateWithParams { get; }
        public Func<ChowValue> ValueReturnDelegate { get; }
        public Action VoidDelegate { get;  }
        public Action<ChowValue[]> VoidDelegateWithParams { get; }
        
        
        public bool IsVoid { get; }
        
        public BuiltInDefinition(
            string name, 
            int minimumArguments, 
            int maximumArguments, 
            bool isVoid, 
            Func<ChowValue[], ChowValue> valueReturnDelegateWithParams, 
            Func<ChowValue> valueReturnDelegate, 
            Action voidDelegate, 
            Action<ChowValue[]> voidDelegateWithParams)
        {
            Name = name;
            MinimumArguments = minimumArguments;
            MaximumArguments = maximumArguments;
            IsVoid = isVoid;
            ValueReturnDelegateWithParams = valueReturnDelegateWithParams;
            ValueReturnDelegate = valueReturnDelegate;
            VoidDelegate = voidDelegate;
            VoidDelegateWithParams = voidDelegateWithParams;
        }
    }
}
