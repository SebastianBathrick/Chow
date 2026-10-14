using System;
using Chow.Interpreter.Exceptions;
using Chow.SourceData;
using Chow.StandardLibrary.BuiltIns;

namespace Chow
{
    /// <summary>The entry point for executing Chow source code.</summary>
    public static class ChowEngine
    {
        /// <summary>
        /// Compiles and executes a piece of Chow source code and returns its result. The source code
        /// is compiled before the scope is touched, so source code that fails to compile leaves the
        /// scope unchanged.
        /// </summary>
        /// <param name="srcCode">The Chow source code to execute.</param>
        /// <param name="scope">An optional <see cref="ChowScope"/> the code runs in. Its variables
        /// are available to the code, and any variables the code defines are stored in it. When
        /// <c>null</c>, a fresh scope is used.</param>
        /// <param name="useBuiltIns">Whether the built-in functions are made available to the
        /// code.</param>
        /// <returns>The result of the last evaluated expression statement, or the Chow <c>None</c>
        /// object if no expression statement was evaluated.</returns>
        /// <exception cref="CompileException">The source code could not be compiled.</exception>
        public static ChowObject Run(string srcCode, IChowObject scope = null, bool useBuiltIns = true)
        {
            return Run(Compile(srcCode), scope, useBuiltIns);
        }

        /// <summary>
        /// Executes an already compiled <see cref="ChowProgram"/> and returns its result. The same
        /// program can be run any number of times, against the same scope or different ones.
        /// </summary>
        /// <param name="program">The compiled program to execute.</param>
        /// <param name="scope">An optional <see cref="ChowScope"/> the program runs in. Its variables
        /// are available to the program, and any variables the program defines are stored in it.
        /// When <c>null</c>, a fresh scope is used.</param>
        /// <param name="useBuiltIns">Whether the built-in functions are made available to the
        /// program.</param>
        /// <returns>The result of the last evaluated expression statement, or the Chow <c>None</c>
        /// object if no expression statement was evaluated.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="program"/> is <c>null</c>.</exception>
        public static ChowObject Run(ChowProgram program, IChowObject scope = null, bool useBuiltIns = true)
        {
            if (program == null)
            {
                throw new ArgumentNullException(nameof(program));
            }

            var globalScope = SetupGlobalScope(scope, useBuiltIns);
            Interpreter.VirtualMachine.Run(program.BytecodeChunk, globalScope, out var result);
            var resultChowObj = (ChowObject)ApiConverter.Convert(ref result);

            globalScope.AssignVariableValue(SourceObjectConsts.ScopeExpressionName, ref result);
            return resultChowObj;
        }

        /// <summary>
        /// Compiles a piece of Chow source code without executing it, so it can be run any number of
        /// times through <see cref="Run(ChowProgram, IChowObject, bool)"/> without being compiled
        /// again.
        /// </summary>
        /// <param name="srcCode">The Chow source code to compile.</param>
        /// <returns>The compiled program.</returns>
        /// <exception cref="CompileException">The source code could not be compiled.</exception>
        public static ChowProgram Compile(string srcCode)
        {
            return new ChowProgram(Interpreter.VirtualMachine.Compile(srcCode));
        }
        
        
        internal static IChowObject Call(ref SourceValue func, SourceValue[] args)
        {
            Interpreter.VirtualMachine.RunFunctionCall(ref func, out var returnVal, args);
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
