using Chow.Interpreter.Exceptions;
using Chow.Interpreter.State;
using Chow.Interpreter.Values;
using System;
using System.Collections.Generic;
namespace Chow.Interpreter
{
    /// <summary>
    /// Represents a managed global scope where global variables are declared via an instance’s
    /// indexer or using source code variable declarations (or function definitions) executed using
    /// the instance’s public methods. 
    /// </summary>
    public sealed class ChowModule
    {
        readonly Scope _globalScope;
        readonly Dictionary<BuiltInType, Func<ChowValue[], ChowValue>> _builtInOverrides
            = new Dictionary<BuiltInType, Func<ChowValue[], ChowValue>>();

        #region Properties
        /// <summary>
        /// <para>
        /// Returns the value of, or assigns a value to, a variable bound to a string name.
        /// </para>
        /// <para>
        /// When getting a variable value, if it is undefined, then <b>null</b> will be returned.
        /// </para>
        /// <para>
        /// When setting a variable value, if the variable is undefined, a new variable is declared
        /// and initialized to the provided value.
        /// </para>
        /// <para>
        /// <b>Note:</b> Variables can store functions that are first-class objects. When the
        /// interpreter encounters a function definition, a new variable is instantiated and bound
        /// to the name specified in the signature. During runtime, an invocation operator directly
        /// following a function name signals to the interpreter that the variable contains a function;
        /// if so, the variable will interpret that function using any argument(s) provided nested in the
        /// invocation operator. If the variable is NOT a function, then an exception will be thrown.
        /// </para>
        /// </summary>
        /// <param name="name">Name of the variable or function to set/get.</param>
        public object this[string name]
        {
            get
            {
                if (_globalScope.ContainsVariable(name))
                {
                    return _globalScope.GetVariableValue(name).AsType<object>();
                }

                return null;
            }
            set
            {
                var chowValue = new ChowValue(value);

                if (IsValidChowName(name))
                {
                    _globalScope.AssignVariableValue(name, chowValue);
                    return;
                }
                
                throw new GlobalAccessException(name, 
                    $"'{name}' does not abide by the rules for variable names");
            }
        }

        #endregion

        /// <summary>Initializes a ChowModule with all built-in functions enabled.</summary>
        public ChowModule(bool isBuiltInsEnabled = true)
        {
            _globalScope = new Scope();
            ToggleBuiltIns(isBuiltInsEnabled);
        }


        /// <summary>  Compiles and interprets a string containing Chow source code. </summary>
        /// <param name="sourceCode">String containing Chow source code, whitespace, or null.</param>
        /// <returns><see cref="ChowValue.None"/>, or the result of the last expression statement
        /// interpreted, if there was one defined in <paramref name="sourceCode"/>, and it is not null.</returns>
        public ChowValue Execute(string sourceCode)
        {
            return ChowEngine.ExecuteModuleCode(sourceCode, _globalScope);
        }

        #region Global Scope API Methods

        /// <summary>
        /// Invokes Chow function or an interop function with the provided arguments.
        /// </summary>
        /// <param name="functionName">The name of the variable the target function was assigned to.</param>
        /// <param name="args">Boxed host language values to pass to the function as arguments.</param>
        /// <returns>If the target was non-void function then this method will return target function's
        /// returned <see cref="ChowValue"/>. </returns>
        /// <exception cref="GlobalAccessException"></exception>
        public ChowValue InvokeGlobal(string functionName, params object[] args)
        {
            if (IsGlobal(functionName))
            {
                var convertedArgs = ConvertToChowValues(args);
                return ChowEngine.InvokeChowFunction(_globalScope, functionName, convertedArgs);
            }

            throw new GlobalAccessException(functionName, $"'{functionName}' is not defined in the global scope");
        }

        /// <summary>
        /// <para>
        /// Returns <see langword="true"/> if <paramref name="name"/> is defined in the module's
        /// global scope.
        /// </para>
        /// <para>
        /// If the name does not follow the rules for variable names in Chow. As 
        /// </para>
        /// </summary>
        /// <exception cref="GlobalAccessException">Thrown when <paramref name="name"/> is null.</exception>
        public bool IsGlobal(string name)
        {
            return _globalScope.ContainsVariable(name);
        }

        #endregion

        #region Global Scope API Helper Methods

        static ChowValue[] ConvertToChowValues(object[] args)
        {
            if (args == null || args.Length == 0)
            {
                // TODO: Consider returning null instead of an empty array
                return Array.Empty<ChowValue>();
            }

            var result = new ChowValue[args.Length];

            for (var i = 0; i < args.Length; i++)
            {
                result[i] = new ChowValue(args[i]);
            }

            return result;
        }

        // Returns true if essentially the name abides by the same name rules as Python.
        static bool IsValidChowName(string name)
        {
            var first = name[0];
            if (!IsLetterChar(first) && first != '_')
            {
                return false;
            }

            for (var i = 1; i < name.Length; i++)
            {
                var c = name[i];
                if (!IsLetterChar(c) && !IsDigitChar(c) && c != '_')
                {
                    return false;
                }
            }

            return true;
        }

        // Avoids char.IsLetter(char) because it is slower due to large Unicode checks
        static bool IsLetterChar(char c)
        {
            return c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z';
        }

        
        // Consult the documentation for IsLetterChar, because this function exists for a similar reason
        static bool IsDigitChar(char c)
        {
            return c >= '0' && c <= '9';
        }

        #endregion

        #region Built-Ins API Methods

        public void ToggleBuiltIns(bool isEnabling, params BuiltInType[] types)
        {
            if (types == null || types.Length == 0)
            {
                types = (BuiltInType[])Enum.GetValues(typeof(BuiltInType));
            }
            
            foreach (var type in types)
            {
                ToggleOneBuiltIn(isEnabling, type);
            }
        }
        
        void ToggleOneBuiltIn(bool isEnabling, BuiltInType type)
        {

            var signature = BuiltIns.SignitureOf(type);
            var builtInName = signature.Name;

            if (isEnabling)
            {
                var implVarValue = new ChowValue(signature.Implementation);
                
                // If the variable does not exist, a new variable will be declared & initialized.
                _globalScope.AssignVariableValue(builtInName, implVarValue);
            }
            else
            {
                // If the variable does not exist, the variable is already disabled so ignore it.
                _globalScope.TryRemoveVariable(builtInName);
            }
        }

        /// <summary>
        /// Overrides the implementation of a built-in function.
        /// 
        /// <para>Supported delegate types:</para>
        /// <list type="bullet">
        /// 
        /// <item><see cref="Func{TResult}"/> where TResult is <see cref="ChowValue"/> —
        /// zero-argument function</item>
        /// 
        /// <item><see cref="Func{T, TResult}"/> where T and TResult are <see cref="ChowValue"/> —
        /// single-argument function</item>
        /// 
        ///<item><see cref="Func{T, TResult}"/> where T is <see cref="ChowValue"/>[] and TResult
        /// is <see cref="ChowValue"/> — variadic function</item>
        /// 
        /// <item><see cref="Action"/> — zero-argument action</item>
        /// 
        /// <item><see cref="Action{T}"/> where T is <see cref="ChowValue"/> — single-argument action</item>
        /// 
        /// <item><see cref="Action{T}"/> where T is <see cref="ChowValue"/>[] — variadic action</item>
        /// 
        /// </list>
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builtIn"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="builtIn"/> is not one of the supported delegate types.</exception>
        public void SetBuiltIn(BuiltInType type, Delegate builtIn)
        {
            if (builtIn == null)
            {
                throw new ArgumentNullException(nameof(builtIn));
            }

            Func<ChowValue[], ChowValue> impl;

            switch (builtIn)
            {
                case Func<ChowValue[], ChowValue> variadicFunc:
                    impl = variadicFunc;
                    break;

                case Func<ChowValue, ChowValue> unaryFunc:
                    BuiltIns.SignitureOf(type).RequireFixedArity(1, "Func<ChowValue, ChowValue>");
                    impl = args => unaryFunc(args[0]);
                    break;

                case Func<ChowValue> nullaryFunc:
                    BuiltIns.SignitureOf(type).RequireFixedArity(0, "Func<ChowValue>");
                    impl = _ => nullaryFunc();
                    break;

                case Action<ChowValue[]> variadicAction:
                    impl = args => { variadicAction(args); return ChowValue.None; };
                    break;

                case Action<ChowValue> unaryAction:
                    BuiltIns.SignitureOf(type).RequireFixedArity(1, "Action<ChowValue>");
                    impl = args => { unaryAction(args[0]); return ChowValue.None; };
                    break;

                case Action nullaryAction:
                    BuiltIns.SignitureOf(type).RequireFixedArity(0, "Action");
                    impl = _ => { nullaryAction(); return ChowValue.None; };
                    break;

                default:
                    throw new ArgumentException(
                        $"Unsupported delegate type '{builtIn.GetType()}'. " +
                        $"Expected one of: {nameof(Func<ChowValue[], ChowValue>)}, {nameof(Func<ChowValue, ChowValue>)}, " +
                        $"{nameof(Func<ChowValue>)}, {nameof(Action<ChowValue[]>)}, {nameof(Action<ChowValue>)}, or {nameof(Action)}.",
                        nameof(builtIn));
            }

            _builtInOverrides[type] = impl;
            ToggleOneBuiltIn(isEnabling: true, type);
        }

        #endregion

    }
}
