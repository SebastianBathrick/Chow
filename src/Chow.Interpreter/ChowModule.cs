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
                return varUnion.GetTaggedValue();
            }

            set
            {
                ValidateGlobalName(name);

                if (_moduleScope == null)
                {
                    _moduleScope = new ModuleScope();
                }

                TaggedUnion varUnion;

                if (value is ChowValue chowVal)
                {
                    // Get value inside ChowValue because ChowValues objects CANNOT be stored inside a TaggedUnion
                    // TODO: Add error checking to ensure that the object field in TaggedUnion can never be a ChowValue
                    varUnion = ChowValueConverter.ToTaggedUnion(chowVal);
                }
                else
                {
                    // The object type is determined & a new TaggedUnion containing the value & appropriate tag is returned
                    // Note that if value is null then the static field TaggedUnion.None will be returned
                    varUnion = TaggedUnion.CreateWithValue(value);
                }


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
            return ChowValueConverter.ToChowValue(varUnion);
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
            var varUnion = ChowValueConverter.ToTaggedUnion(value);

            // Method either assigns a new value to an existing variable, or declares & initializes a new variable
            _moduleScope.AssignVariableValue(name, varUnion);
        }

        #endregion

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
            var vm = new VirtualMachine(chunk, _moduleScope);

            // The module scope will now contain any global variables & functions defined in the source code
            _moduleScope = vm.EvaluateChunk();
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
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        }

        #endregion
    }
}
