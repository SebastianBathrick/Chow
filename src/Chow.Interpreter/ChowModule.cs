using Chow.Interpreter.Exceptions;
using Chow.Interpreter.State;
using Chow.Interpreter.Values;
using Chow.Interpreter.Values.DataTypes;
using System;
using System.Collections.Generic;
namespace Chow.Interpreter
{
    /// <summary>
    /// An executable containing a global scope that contains variables and functions that are accessible
    /// via the public API.
    /// </summary>
    public class ChowModule
    {
        readonly string _name;
        readonly Scope _globalScope;
        readonly Dictionary<BuiltInType, Func<ChowValue[], ChowValue>> _overrides
            = new Dictionary<BuiltInType, Func<ChowValue[], ChowValue>>();

        #region Indexer

        /// <summary>Gets the values of and declares variables and functions declared/defined in the global scope.</summary>
        /// <param name="name">Name of variable or function to set/get.</param>
        /// <exception cref="GlobalAccessException">Thrown if the value being retrieved is undefined in the global scope.</exception>
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

        public ChowValue GetGlobal(string name)
        {
            throw new NotImplementedException();
        }

        public void SetGlobal(string name, ChowValue value)
        {
            throw new NotImplementedException();
        }

        public bool IsGlobal(string name)
        {
            throw new NotImplementedException();
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

        public void SetBuiltIn(BuiltInType type, Func<ChowValue[], ChowValue> builtInFunc)
        {
            if (builtInFunc == null)
            {
                throw new ArgumentNullException(nameof(builtInFunc));
            }

            _overrides[type] = builtInFunc;
            SeedBuiltIn(type);
        }

        public void SetBuiltIn(BuiltInType type, Func<ChowValue, ChowValue> builtInFunc)
        {
            if (builtInFunc == null)
            {
                throw new ArgumentNullException(nameof(builtInFunc));
            }

            BuiltIns.DefinitionOf(type).RequireFixedArity(1, "Func<ChowValue, ChowValue>");
            SetBuiltIn(type, args => builtInFunc(args[0]));
        }

        public void SetBuiltIn(BuiltInType type, Func<ChowValue> builtInFunc)
        {
            if (builtInFunc == null)
            {
                throw new ArgumentNullException(nameof(builtInFunc));
            }

            BuiltIns.DefinitionOf(type).RequireFixedArity(0, "Func<ChowValue>");
            SetBuiltIn(type, (ChowValue[] _) => builtInFunc());
        }

        public void SetBuiltIn(BuiltInType type, Action<ChowValue[]> builtInAction)
        {
            if (builtInAction == null)
            {
                throw new ArgumentNullException(nameof(builtInAction));
            }

            SetBuiltIn(type, args =>
            {
                builtInAction(args);
                return ChowValue.None;
            });
        }

        public void SetBuiltIn(BuiltInType type, Action<ChowValue> builtInAction)
        {
            if (builtInAction == null)
            {
                throw new ArgumentNullException(nameof(builtInAction));
            }

            BuiltIns.DefinitionOf(type).RequireFixedArity(1, "Action<ChowValue>");
            SetBuiltIn(type, args =>
            {
                builtInAction(args[0]);
                return ChowValue.None;
            });
        }

        public void SetBuiltIn(BuiltInType type, Action builtInAction)
        {
            if (builtInAction == null)
            {
                throw new ArgumentNullException(nameof(builtInAction));
            }

            BuiltIns.DefinitionOf(type).RequireFixedArity(0, "Action");
            SetBuiltIn(type, (ChowValue[] _) =>
            {
                builtInAction();
                return ChowValue.None;
            });
        }

        #endregion

    }
}
