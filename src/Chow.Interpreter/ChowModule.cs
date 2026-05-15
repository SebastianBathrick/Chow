using System.Collections.Generic;
using Chow.Interpreter.State.Scopes;
using Chow.Interpreter.State.Values;
using Chow.Interpreter.Exceptions;
using Chow.Interpreter.Tokens;
using Chow.Interpreter.Values;

namespace Chow.Interpreter
{
    public class ChowModule
    {
        IScope _moduleScope;

        #region Global Scope Access

        public object this[string name]
        {
            get
            {
                ValidateGlobalExists(name);
                var varUnion = _moduleScope.GetVariableValue(name);

                // Gets the C#-typed value stored in the TaggedUnion instance
                return ApiConverter.ToObject(varUnion);
            }

            set
            {
                ValidateGlobalName(name);

                if (_moduleScope == null)
                {
                    _moduleScope = new ModuleScope();
                }

                var varUnion = ApiConverter.ToTaggedUnion(value);

                // Method either assigns a new value to an existing variable, or declares & initializes a new variable
                _moduleScope.AssignVariableValue(name, varUnion);
            }
        }

        public bool ContainsGlobal(string name)
        {
            return _moduleScope != null && _moduleScope.IsVariableDefined(name);
        }

        public ChowValue GetGlobal(string name)
        {
            ValidateGlobalExists(name);
            var varUnion = _moduleScope.GetVariableValue(name);

            // Extracts the value from the TaggedUnion and converts it to a ChowValue to return
            return ApiConverter.ToChowValue(varUnion);
        }

        public void SetGlobal(string name, ChowValue value)
        {
            ValidateGlobalName(name);

            if (value == null)
            {
                throw new GlobalAccessException(name, "Cannot assign null to a global variable");
            }

            if (_moduleScope == null)
            {
                _moduleScope = new ModuleScope();
            }

            // Extracts the value from ChowValue and creates a new TaggedUnion containing the value & appropriate tag
            var varUnion = ApiConverter.ToTaggedUnion(value);

            // Method either assigns a new value to an existing variable, or declares & initializes a new variable
            _moduleScope.AssignVariableValue(name, varUnion);
        }

        #endregion

        public void ImportBuiltIns()
        {
            foreach ((string name, object obj) func in BuiltIns.GetFunctions())
            {
                this[func.name] = func.obj;
            }
        }
        
        public void Execute(string sourceCode)
        {
            // Source code that is null, empty, or whitespace is treated the same by the Scanner (it does a null check)
            // The rest of the pipeline does so as well to keep state consistent (e.g. _moduleScope)
            var scanner = new Scanner(sourceCode);
            var tokens = scanner.ScanTokens();

            var parser = new Parser(tokens);
            var syntaxTreeRoot = parser.BuildTree();

            var compiler = new Compiler(syntaxTreeRoot);
            var chunk = compiler.CompileRoot();
            
            // Executes the chunk with the provided module scope, or if null, a new one
            var vm = new VirtualMachine(_moduleScope, chunk);

            // The module scope will now contain any global variables & functions defined in the source code
            _moduleScope = vm.EvaluateChunk();
        }

        public ChowValue CallFunction(string functionName, params object[] arguments)
        {
            ValidateGlobalName(functionName);
            ValidateGlobalExists(functionName);

            var vm = new VirtualMachine(_moduleScope);
            var taggedUnionArgs = new List<TaggedUnion>();

            if (arguments != null)
            {
                foreach (var argument in arguments)
                {
                    var taggedUnionArg = ApiConverter.ToTaggedUnion(argument);
                    taggedUnionArgs.Add(taggedUnionArg);
                }
            }

            var returnedUnion = vm.CallGlobalFunction(functionName, taggedUnionArgs);
            return ApiConverter.ToChowValue(returnedUnion);
        }
        
        #region Helper Methods

        void ValidateGlobalExists(string name)
        {
            if (!ContainsGlobal(name))
            {
                throw new GlobalAccessException(name, $"Global name '{name}' is not defined");
            }
        }

        static void ValidateGlobalName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new GlobalAccessException(name, "Global variable names cannot be null, empty, or whitespace");
            }

            if (ReservedKeywords.Contains(name))
            {
                throw new GlobalAccessException(name, "Global variable names cannot be reserved keywords");
            }

            if (!IsLetter(name[0]) && name[0] != '_')
            {
                throw new GlobalAccessException(name, "Global variable names must start with a letter or underscores");
            }

            // Skip first char
            for (var i = 1; i < name.Length; i++)
            {
                if (!IsLetter(name[i]) && !IsDigit(name[i]) && name[i] != '_')
                {
                    throw new GlobalAccessException(name, "Global variable names can only contain letters, digits, or underscores");
                }
            }
        }

        static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        static bool IsLetter(char c)
        {
            return c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z';
        }

        #endregion
    }
}
