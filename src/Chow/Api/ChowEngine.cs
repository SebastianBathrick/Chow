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
        /// <param name="settings">The settings for this run, such as whether the built-in functions
        /// are available and the recursion limit. The default value uses the default for each
        /// setting.</param>
        /// <returns>The result of the last evaluated expression statement, or the Chow <c>None</c>
        /// object if no expression statement was evaluated.</returns>
        /// <exception cref="CompileException">The source code could not be compiled.</exception>
        /// <exception cref="ArgumentException"><paramref name="scope"/> was previously run with a
        /// different <see cref="ChowRunSettings.ExcludeBuiltIns"/> value.</exception>
        public static ChowObject Run(string srcCode, IChowObject scope = null, ChowRunSettings settings = default)
        {
            return Run(Compile(srcCode), scope, settings);
        }

        /// <summary>
        /// Executes an already compiled <see cref="ChowProgram"/> and returns its result. The same
        /// program can be run any number of times, against the same scope or different ones.
        /// </summary>
        /// <param name="program">The compiled program to execute.</param>
        /// <param name="scope">An optional <see cref="ChowScope"/> the program runs in. Its variables
        /// are available to the program, and any variables the program defines are stored in it.
        /// When <c>null</c>, a fresh scope is used.</param>
        /// <param name="settings">The settings for this run, such as whether the built-in functions
        /// are available and the recursion limit. The default value uses the default for each
        /// setting.</param>
        /// <returns>The result of the last evaluated expression statement, or the Chow <c>None</c>
        /// object if no expression statement was evaluated.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="program"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="scope"/> was previously run with a
        /// different <see cref="ChowRunSettings.ExcludeBuiltIns"/> value.</exception>
        public static ChowObject Run(ChowProgram program, IChowObject scope = null, ChowRunSettings settings = default)
        {
            if (program == null)
            {
                throw new ArgumentNullException(nameof(program));
            }

            var globalScope = SetupGlobalScope(scope, settings);
            Interpreter.VirtualMachine.Run(program.BytecodeChunk, globalScope, out var result);
            var resultChowObj = (ChowObject)ApiConverter.Convert(ref result);

            globalScope.AssignVariableValue(SourceObjectConsts.ScopeExpressionName, ref result);
            return resultChowObj;
        }

        /// <summary>
        /// Compiles a piece of Chow source code without executing it, so it can be run any number of
        /// times through <see cref="Run(ChowProgram, IChowObject, ChowRunSettings)"/> without being compiled
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

        static Scope SetupGlobalScope(IChowObject apiScope, ChowRunSettings settings)
        {
            var scope = apiScope != null ? ExtractScope(apiScope) : new Scope();
            ApplyRunSettings(scope, settings);
            return scope;
        }

        static void ApplyRunSettings(Scope scope, ChowRunSettings settings)
        {
            var previousSettings = scope.RunSettings;

            // A scope's first run decides whether it has built-ins, since they can't be taken back out.
            if (previousSettings.HasValue && previousSettings.Value.ExcludeBuiltIns != settings.ExcludeBuiltIns)
            {
                throw new ArgumentException(
                    $"The scope was first run with {nameof(ChowRunSettings.ExcludeBuiltIns)} set to "
                    + $"{previousSettings.Value.ExcludeBuiltIns}, so every run on it must use the same value",
                    nameof(settings));
            }

            if (!previousSettings.HasValue && !settings.ExcludeBuiltIns)
            {
                AddBuiltInsToScope(scope);
            }

            // Stored on every run, so host calls into the scope's functions use the latest recursion limit.
            scope.RunSettings = settings;
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

        static void AddBuiltInsToScope(Scope scope)
        {
            var invocablesList = BuiltInFunctions.NamedInvocableObjects;

            for(var i = 0; i < invocablesList.Count; i++)
            {
                var chowValue = new SourceValue(invocablesList[i].callableObject);
                scope.AssignVariableValue(invocablesList[i].name, ref chowValue);
            }
        }
    }
}
