using System;
using System.Collections.Generic;
using Chow.Sandboxing;
using Chow.SourceData;
using Chow.StandardLibrary.BuiltIns;

namespace Chow
{
    /// <summary>The entry point for executing Chow source code.</summary>
    public static class ChowEngine
    {
        /// <summary>Executes a piece of Chow source code and returns its result.</summary>
        /// <param name="srcCode">The Chow source code to execute.</param>
        /// <param name="scope">An optional <see cref="ChowScope"/> the code runs in. Its variables
        /// are available to the code, and any variables the code defines are stored in it. When
        /// <c>null</c>, a fresh scope is used.</param>
        /// <param name="useBuiltIns">Whether the built-in functions are made available to the
        /// code.</param>
        /// <returns>The result of the last evaluated expression statement, or the Chow <c>None</c>
        /// object if no expression statement was evaluated.</returns>
        public static ChowObject Run(
            string srcCode, 
            IChowObject scope = null, 
            bool useBuiltIns = true, 
            params InterpreterBehavior[] behaviors)
        {
            var globalScope = SetupGlobalScope(scope, useBuiltIns);
            Interpreter.VirtualMachine.Run(srcCode, globalScope, out var result);
            var resultChowObj = (ChowObject)ApiConverter.Convert(ref result);

            globalScope.AssignVariableValue(SourceObjectConsts.ScopeExpressionName, ref result);
            return resultChowObj;
        }
        
        
        internal static IChowObject Call(ref SourceValue func, IChowObject[] args)
        {
            Interpreter.VirtualMachine.RunFunctionCall(ref func, out var returnVal, ApiConverter.Convert(args));
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
                : ((SourceScope)srcVal.ToISourceObject()).InternalInternalScope;
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
