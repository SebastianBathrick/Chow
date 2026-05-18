using Chow.Interpreter.Exceptions;
using Chow.Interpreter.State;
using Chow.Interpreter.Values;
using System;
namespace Chow.Interpreter
{
    /// <summary>
    /// An executable containing a global scope that contains variables and functions that are accessible
    /// via the public API.
    /// </summary>
    public class ChowModule
    {
        readonly Scope _globalScope = new Scope();

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
        public ChowModule()
        {
            foreach (var type in BuiltIns.AllTypes)
            {
                _globalScope.AssignVariableValue(BuiltIns.NameOf(type), new ChowValue(BuiltIns.DefaultOf(type)));
            }
        }


        /// <summary>Compiles and executes a string containing Chow source code.</summary>
        /// <param name="sourceCode">String containing Chow source code or null.</param>
        public ChowValue Execute(string sourceCode)
        {
            var scanner = new Scanner(sourceCode);
            var tokens = scanner.ScanTokens();

            var parser = new Parser(tokens);
            var syntaxTreeRoot = parser.BuildTree();

            var semanticAnalyzer = new SemanticAnalyzer(syntaxTreeRoot);
            semanticAnalyzer.Analyze();
               
            var compiler = new Compiler(syntaxTreeRoot);
            var chunk = compiler.CompileRoot();

            var vm = new VirtualMachine(_globalScope, chunk);
            return vm.EvaluateChunk();
        }

        public ChowValue Call(string functionName, params object[] args)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public void EnableAllBuiltIns()
        {
            throw new NotImplementedException();
        }

        public void DisableBuiltIns(params BuiltInType[] types)
        {
            throw new NotImplementedException();
        }

        public void DisableAllBuiltIns()
        {
            throw new NotImplementedException();
        }

        public void SetBuiltIn(BuiltInType type, Func<ChowValue[], ChowValue> builtInFunc)
        {
            throw new NotImplementedException();
        }

        public void SetBuiltIn(BuiltInType type, Func<ChowValue, ChowValue> builtInFunc)
        {
            throw new NotImplementedException();
        }

        public void SetBuiltIn(BuiltInType type, Func<ChowValue> builtInFunc)
        {
            throw new NotImplementedException();
        }

        public void SetBuiltIn(BuiltInType type, Action<ChowValue[]> builtInAction)
        {
            throw new NotImplementedException();
        }

        public void SetBuiltIn(BuiltInType type, Action<ChowValue> builtInAction)
        {
            throw new NotImplementedException();
        }

        public void SetBuiltIn(BuiltInType type, Action builtInAction)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
