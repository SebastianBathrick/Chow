using Chow.Interpreter.Exceptions;
using Chow.Interpreter.State;
using Chow.Interpreter.Tokens;
using Chow.Interpreter.Values;
using Chow.Interpreter.Values.DataTypes;
using System;
using System.Collections.Generic;
namespace Chow.Interpreter
{
    /// <summary>
    /// An executable containing a global scope that contains variables and functions that are
    /// accessible via the public API.
    /// </summary>
    public class ChowModule
    {
        readonly string _name;
        readonly Scope _globalScope;
        readonly Dictionary<BuiltInType, Func<ChowValue[], ChowValue>> _overrides
            = new Dictionary<BuiltInType, Func<ChowValue[], ChowValue>>();

        #region Properties

        public string Name => _name;

        /// <summary>
        /// Gets the values of and declares variables and functions declared/defined in the
        /// global scope of this module.
        /// </summary>
        /// <param name="name">Name of variable or function to set/get.</param>
        /// <exception cref="GlobalAccessException">Thrown if the value being retrieved is undefined
        /// in the global scope.</exception>
        public object this[string name]
        {
            get
            {
                // TODO: Add logic to check if the variable name is valid
                if (_globalScope.IsVariableDefined(name))
                {
                    return _globalScope.GetVariableValue(name).AsType<object>();
                }

                throw new GlobalAccessException(name, $"undefined name '{name}'");
            }
            set
            {
                var chowValue = new ChowValue(value);
                _globalScope.AssignVariableValue(name, chowValue);
            }
        }

        #endregion

        /// <summary>Initializes a ChowModule with built-in functions.</summary>
        public ChowModule(string name = nameof(ChowModule))
        {
            _name = name;
            _globalScope = new Scope();

            foreach (var type in BuiltIns.AllTypes)
            {
                SeedBuiltIn(type);
            }
        }

        void SeedBuiltIn(BuiltInType type)
        {
            var def = BuiltIns.DefinitionOf(type);
            Func<ChowValue[], ChowValue> rawImpl;

            if (!_overrides.TryGetValue(type, out rawImpl))
            {
                rawImpl = def.Implementation;
            }

            _globalScope.AssignVariableValue(def.Name, new ChowValue(def.WrapWithArityCheck(rawImpl)));
        }


        /// <summary>Compiles and executes a string containing Chow source code.</summary>
        /// <param name="sourceCode">String containing Chow source code or null.</param>
        public ChowValue Execute(string sourceCode)
        {
            return ChowEngine.ExecuteModuleCode(sourceCode, _globalScope);
        }

        /// <summary>Invokes a callable stored in the module's global scope and returns its result.</summary>
        /// <param name="functionName">Name of the global to call (interop delegate or Chow closure).</param>
        /// <param name="args">Host arguments; each is converted via <see cref="ChowValue(object)"/>.</param>
        /// <exception cref="GlobalAccessException">The name is not defined in the module's global scope.</exception>
        /// <exception cref="TypeException">The global exists but is not callable, or arity does not match.</exception>
        public ChowValue Call(string functionName, params object[] args)
        {
            if (!_globalScope.IsVariableDefined(functionName))
            {
                throw new GlobalAccessException(functionName, $"'{functionName}' is undefined and cannot be called");
            }

            var callee = _globalScope.GetVariableValue(functionName);
            var chowArgs = ConvertArgs(args);

            if (callee.IsOfType<Func<ChowValue[], ChowValue>>())
            {
                return callee.CallInterop(chowArgs);
            }

            if (callee.IsOfType<Closure>())
            {
                return ChowEngine.CallModuleFunction(_globalScope, functionName, chowArgs);
            }

            throw new TypeException($"'{functionName}' is a {callee.DataType} which is not callable");
        }

        static ChowValue[] ConvertArgs(object[] args)
        {
            if (args == null || args.Length == 0)
            {
                return Array.Empty<ChowValue>();
            }

            var result = new ChowValue[args.Length];

            for (var i = 0; i < args.Length; i++)
            {
                result[i] = new ChowValue(args[i]);
            }

            return result;
        }

        #region Global Scope API Methods

        /// <summary>
        /// Returns the <see cref="ChowValue"/> bound to <paramref name="name"/> in the module's
        /// global scope.
        /// </summary>
        /// <exception cref="GlobalAccessException">Thrown when <paramref name="name"/> is <see
        /// langword="null"/>, empty, or whitespace; is a reserved keyword; does not satisfy Chow
        /// identifier rules; or is not defined in the global scope.</exception>
        public ChowValue GetGlobal(string name)
        {
            ValidateIdentifier(name);

            if (!_globalScope.IsVariableDefined(name))
            {
                throw new GlobalAccessException(name, $"undefined name '{name}'");
            }

            return _globalScope.GetVariableValue(name);
        }

        /// <summary>
        /// Binds <paramref name="name"/> to <paramref name="value"/> in the module's global scope,
        /// creating or overwriting the binding.
        /// </summary>
        /// <exception cref="GlobalAccessException">
        /// Thrown when <paramref name="name"/> is <see langword="null"/>, empty, or whitespace; is
        /// a reserved keyword; or does not satisfy Chow identifier rules.</exception>
        public void SetGlobal(string name, ChowValue value)
        {
            ValidateIdentifier(name);
            _globalScope.AssignVariableValue(name, value);
        }

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="name"/> is defined in the module's
        /// global scope.
        /// </summary>
        /// <exception cref="GlobalAccessException">Thrown when <paramref name="name"/> is <see
        /// langword="null"/>, empty, or whitespace; is a reserved keyword; or does not satisfy
        /// Chow identifier rules.</exception>
        public bool IsGlobal(string name)
        {
            ValidateIdentifier(name);
            return _globalScope.IsVariableDefined(name);
        }

        static void ValidateIdentifier(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new GlobalAccessException(name, "name must not be null, empty, or whitespace");
            }

            if (ReservedKeywords.Contains(name))
            {
                throw new GlobalAccessException(name, $"'{name}' is a reserved keyword and cannot be used as a variable name");
            }

            if (!IsValidChowIdentifier(name))
            {
                throw new GlobalAccessException(name, $"'{name}' is not a valid Chow identifier");
            }
        }

        static bool IsValidChowIdentifier(string name)
        {
            var first = name[0];
            if (!IsAlpha(first) && first != '_')
            {
                return false;
            }

            for (var i = 1; i < name.Length; i++)
            {
                var c = name[i];
                if (!IsAlpha(c) && !IsDigit(c) && c != '_')
                {
                    return false;
                }
            }

            return true;
        }

        static bool IsAlpha(char c)
        {
            return c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z';
        }

        static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        #endregion

        #region Built-Ins API Methods

        public void EnableBuiltIns(params BuiltInType[] types)
        {
            if (types == null)
            {
                return;
            }

            foreach (var t in types)
            {
                SeedBuiltIn(t);
            }
        }

        public void EnableAllBuiltIns()
        {
            foreach (var t in BuiltIns.AllTypes)
            {
                SeedBuiltIn(t);
            }
        }

        public void DisableBuiltIns(params BuiltInType[] types)
        {
            if (types == null)
            {
                return;
            }

            foreach (var t in types)
            {
                _globalScope.RemoveVariable(BuiltIns.NameOf(t));
            }
        }

        public void DisableAllBuiltIns()
        {
            foreach (var t in BuiltIns.AllTypes)
            {
                _globalScope.RemoveVariable(BuiltIns.NameOf(t));
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
                    BuiltIns.DefinitionOf(type).RequireFixedArity(1, "Func<ChowValue, ChowValue>");
                    impl = args => unaryFunc(args[0]);
                    break;

                case Func<ChowValue> nullaryFunc:
                    BuiltIns.DefinitionOf(type).RequireFixedArity(0, "Func<ChowValue>");
                    impl = _ => nullaryFunc();
                    break;

                case Action<ChowValue[]> variadicAction:
                    impl = args => { variadicAction(args); return ChowValue.None; };
                    break;

                case Action<ChowValue> unaryAction:
                    BuiltIns.DefinitionOf(type).RequireFixedArity(1, "Action<ChowValue>");
                    impl = args => { unaryAction(args[0]); return ChowValue.None; };
                    break;

                case Action nullaryAction:
                    BuiltIns.DefinitionOf(type).RequireFixedArity(0, "Action");
                    impl = _ => { nullaryAction(); return ChowValue.None; };
                    break;

                default:
                    throw new ArgumentException(
                        $"Unsupported delegate type '{builtIn.GetType()}'. " +
                        $"Expected one of: {nameof(Func<ChowValue[], ChowValue>)}, {nameof(Func<ChowValue, ChowValue>)}, " +
                        $"{nameof(Func<ChowValue>)}, {nameof(Action<ChowValue[]>)}, {nameof(Action<ChowValue>)}, or {nameof(Action)}.",
                        nameof(builtIn));
            }

            _overrides[type] = impl;
            SeedBuiltIn(type);
        }

        #endregion

    }
}
