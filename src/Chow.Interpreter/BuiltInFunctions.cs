using System;
using System.Collections.Generic;
using Chow.Interpreter.Values;

namespace Chow.Interpreter
{
    public static class BuiltInFunctions
    {
        
        static readonly List<BuiltInDefinition> _functions = new List<BuiltInDefinition>()
        {

        };

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
}
