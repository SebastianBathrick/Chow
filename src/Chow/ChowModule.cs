using Chow.Interpreter.Compilation;
using Chow.Interpreter.Evaluation;
using Chow.Interpreter.Exceptions;
using Chow.Interpreter.Hooks;
using Chow.Interpreter.Syntax;
using Chow.Interpreter.Syntax.Trees;
using Chow.Interpreter.Tokens;
using Chow.Interpreter.Values;
using Chow.Interpreter.Values.Internal;
using System.Collections.Generic;
using System;

namespace Chow.Interpreter
{
    public class ChowModule
    {
        List<IHook> _hooks = new List<IHook>();
        ModuleScope _moduleScope;

        public object this[string name]
        {
            get
            {
                ValidateGlobalExists(name);
                TaggedUnion varUnion = _moduleScope.GetVariableValue(name);

                // Gets the C#-typed value stored in the TaggedUnion instance
                return varUnion.GetTaggedValue();
            }

            set
            {
                if (_moduleScope == null)
                {
                    _moduleScope = new ModuleScope();
                }

                // The object type is determined & a new TaggedUnion containing the value & appropriate tag is returned
                // Note that if value is null then the static field TaggedUnion.None will be returned
                TaggedUnion varUnion = TaggedUnion.CreateWithValue(value);

                // Method either assigns a new value to an existing variable, or declares & initializes a new variable
                _moduleScope.AssignVariableValue(name, varUnion);
            }
        }

        public ChowValue GetGlobal(string name)
        {
            ValidateGlobalExists(name);
            TaggedUnion varUnion = _moduleScope.GetVariableValue(name);

            // Extracts the value from the TaggedUnion and converts it to a ChowValue to return
            return ChowValueConverter.ToChowValue(varUnion);
        }

        public void SetGlobal(string name, ChowValue value)
        {
            if ()

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (_moduleScope == null)
            {
                _moduleScope = new ModuleScope();
            }

            // Extracts the value from ChowValue and creates a new TaggedUnion containing the value & appropriate tag
            TaggedUnion varUnion = ChowValueConverter.ToTaggedUnion(value);

            // Method either assigns a new value to an existing variable, or declares & initializes a new variable
            _moduleScope.AssignVariableValue(name, varUnion);
        }

        public void Execute(string sourceCode)
        {
            // Run source code with an environment if this instance has executed code before
            // Otherwise, environment will be null and a new environment will be created for the source code to run in
            Scanner scanner = new Scanner(sourceCode);
            List<Token> tokens = scanner.ScanTokens();

            Parser parser = new Parser(tokens);
            Node syntaxTreeRoot = parser.BuildTree();

            Compiler compiler = new Compiler(syntaxTreeRoot);
            Chunk chunk = compiler.CompileRoot();

            // Get hook for expression statements, if it exists, to pass to the virtual machine for execution
            IHook exprStmtHook = _hooks.Find(h => h is IExpressionStatementHook);

            // Executes the chunk with the provided module scope, or if null, a new one
            VirtualMachine vm = new VirtualMachine(chunk, _moduleScope, exprStmtHook);

            // The module scope will now contain any global variables & functions defined in the source code
            // Each global will contain their final values after execution
            _moduleScope = vm.EvaluateChunk();
        }

        private void ValidateGlobalExists(string name)
        {
            if (_moduleScope != null && _moduleScope.IsVariableDefined(name))
            {
                return;
            }

            throw new GetGlobalException(name);
        }

        public void AddHook(IHook hook)
        {
            if (hook == null)
            {
                throw new ArgumentNullException(nameof(hook));
            }

            _hooks.Add(hook);
        }

        static bool IsValidGlobalName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }    

            if (!IsLetter(name[0]) && name[0] != '_')
            {
                return false;
            }

            // Skip first char
            for (int i = 1; i < name.Length; i++)
            {
                if (!IsLetter(name[i]) && !IsDigit(name[i]) && name[i] != '_')
                {
                    return false;
                }
            }

            return true;
        }

        static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        static bool IsLetter(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        }
    }
}
