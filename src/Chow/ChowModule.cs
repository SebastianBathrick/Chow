using Chow.Interpreter.Compilation;
using Chow.Interpreter.Evaluation;
using Chow.Interpreter.Hooks;
using Chow.Interpreter.Syntax;
using Chow.Interpreter.Syntax.Trees;
using Chow.Interpreter.Tokens;
using Chow.Interpreter.Values;
using Chow.Interpreter.Exceptions;
using System.Collections.Generic;

namespace Chow.Interpreter
{
    public class ChowModule
    {
        List<IExecutionHook> _hooks = new List<IExecutionHook>();
        ChowEnviro _enviro;

        public ChowValue this[string name]
        {
            get
            {
                if (_enviro != null && _enviro.IsVariableDefined(name))
                {
                    TaggedUnion varUnion = _enviro.GetVariableValue(name);
                    return ApiValueConverter.ToApiClassObj(varUnion);
                }

                throw new ApiNameErrorException(name);
            }

            set
            {
                if (_enviro == null)
                {
                    _enviro = new ChowEnviro();
                }

                TaggedUnion varUnion = ApiValueConverter.ToTaggedUnion(value);
                _enviro.AssignVariableValue(name, varUnion);
            }
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
            IExecutionHook exprStmtHook = _hooks.Find(h => h is IExprStatementHook);

            // Executes the chunk with the provided environment, or if null, a new environment
            VirtualMachine vm = new VirtualMachine(chunk, _enviro, exprStmtHook);

            // Return environment with all global variables and their values after executing the chunk
            _enviro = vm.ExecuteChunk();
        }

        public void AddHook(IExecutionHook hook)
        {
            _hooks.Add(hook);
        }
    }
}
