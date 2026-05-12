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

namespace Chow.Interpreter
{
    public class ChowModule
    {
        List<IExecutionHook> _hooks = new List<IExecutionHook>();
        ModuleScope _moduleScope;

        public ChowValue this[string name]
        {
            get
            {
                if (_moduleScope != null && _moduleScope.IsVariableDefined(name))
                {
                    TaggedUnion varUnion = _moduleScope.GetVariableValue(name);
                    return ApiValueConverter.ToApiClassObj(varUnion);
                }

                throw new ChowApiNameErrorException(name);
            }

            set
            {
                if (_moduleScope == null)
                {
                    _moduleScope = new ModuleScope();
                }

                TaggedUnion varUnion = ApiValueConverter.ToTaggedUnion(value);
                _moduleScope.AssignVariableValue(name, varUnion);
            }
        }

        public void Run(string sourceCode)
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

            // Executes the chunk with the provided module scope, or if null, a new one
            VirtualMachine vm = new VirtualMachine(chunk, _moduleScope, exprStmtHook);

            // Return the module scope with all top-level bindings after executing the chunk
            _moduleScope = vm.ExecuteChunk();
        }

        public void AddHook(IExecutionHook hook)
        {
            _hooks.Add(hook);
        }
    }
}
