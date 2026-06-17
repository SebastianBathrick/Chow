using System;
using Chow.Pipelines;
using Chow.SourceData;
using Chow.StandardLibrary.BuiltIns;

namespace Chow
{
    public static class ChowEngine
    {
        public static ChowObject Run(string srcCode, IChowObject scope = null, bool useBuiltIns = true)
        {
            var globalScope = SetupGlobalScope(scope, useBuiltIns);
            Interpreter.Run(srcCode, globalScope, out var result);
            var resultChowObj = (ChowObject)ApiConverter.Convert(ref result);
            
            // This attribute might be removed
            return resultChowObj;
        }
        
        
        internal static IChowObject Call(ref SourceValue func, IChowObject[] args)
        {
            Interpreter.RunFunctionCall(ref func, ApiConverter.Convert(args), out var returnVal);
            return ChowObjectFactory.Create(ref returnVal);
        }

        static Scope SetupGlobalScope(IChowObject apiScope, bool useBuiltIns)
        {
            var scope = apiScope != null ? ExtractScope(apiScope) : new Scope();
            AddBuiltInsToScope(scope, useBuiltIns);
            return scope;
        }

        static Scope ExtractScope(IChowObject apiScope)
        {
            // Scope Extraction Order (Temporary Solution):
            // IChowObject -> SourceValue -> ISourceObject -> SourceScope -> Scope 
            var srcVal = ApiConverter.Convert(apiScope);

            return srcVal.DataType != DataType.Scope 
                ? throw new ArgumentException("Value is not a scope", nameof(apiScope)) 
                : ((SourceScope)srcVal.ToISourceObject()).InternalScope;

        }

        static void AddBuiltInsToScope(Scope scope, bool useBuiltIns)
        {
            // If the scope is not using built-ins, or already had built-ins added
            if (!useBuiltIns || scope.HasBuiltIns)
            {
                return;
            }

            var invocablesList = BuiltInFunctions.NamedInvocableObjects;

            for(var i = 0; i < invocablesList.Count; i++)
            {
                var chowValue = new SourceValue(invocablesList[i].callableObject);
                scope.AssignVariableValue(invocablesList[i].name, ref chowValue);
            }

            scope.HasBuiltIns = true;
        }
    }
}
