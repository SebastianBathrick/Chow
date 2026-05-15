using System.Collections.Generic;
using Chow.Interpreter.State.Scopes;
using Chow.Interpreter.State.Values;
using Chow.Interpreter.Exceptions;
using Chow.Interpreter.Tokens;
using Chow.Interpreter.Values;
using System;

namespace Chow.Interpreter
{
    // TODO: Do big refactor to where the API will create a new module when the current one is null.
    /// <summary>
    /// The primary entry point for embedding the Chow interpreter. Manages a persistent global scope
    /// across multiple <see cref="Execute"/> calls so that variables and functions defined in one call
    /// are available in subsequent calls.
    /// </summary>
    public class ChowModule
    {
        Scope _globalScope;

        #region Global Scope Access

        /// <summary>Gets or sets a global variable by name.</summary>
        /// <remarks>
        /// The getter returns the variable's value as a boxed primitive (<see langword="long"/>,
        /// <see langword="double"/>, <see langword="bool"/>, <see langword="string"/>) or a
        /// <see cref="ChowValue"/> subclass for composite types (list, dict, object, function).
        /// <para>
        /// The setter creates the variable if it does not already exist and does not require a prior
        /// <see cref="Execute"/> call. Accepted value types are <see langword="long"/>,
        /// <see langword="double"/>, <see langword="bool"/>, <see langword="string"/>, any
        /// <see cref="ChowValue"/> subclass, and <see cref="ChowObject"/>.
        /// </para>
        /// </remarks>
        /// <param name="name">The global variable name. Must satisfy the global name rules.</param>
        /// <exception cref="InvalidOperationException">
        /// Getter only: <see cref="Execute"/> has not been called yet.
        /// </exception>
        /// <exception cref="GlobalAccessException">
        /// <paramref name="name"/> is invalid or reserved (getter and setter), or the variable does not
        /// exist (getter only).
        /// </exception>
        public object this[string name]
        {
            get
            {

                ValidateGlobalExists(name, _globalScope);
                var varUnion = _globalScope.GetVariableValue(name);
                return ApiConverter.ToObject(varUnion);
            }

            set
            {
                ValidateGlobalName(name);

                if (_globalScope == null)
                {
                    _globalScope = new Scope();
                }

                var varUnion = ApiConverter.ToTaggedUnion(value);

                // Method either assigns a new value to an existing variable, or declares & initializes a new variable
                _globalScope.AssignVariableValue(name, varUnion);
            }
        }

        /// <summary>
        /// Returns <see langword="true"/> if the named variable exists in the global scope. Safe to call before
        /// <see cref="Execute"/> — returns <see langword="false"/> when no scope has been initialised yet.
        /// </summary>
        /// <param name="name">The global variable name to check.</param>
        public bool ContainsGlobal(string name)
        {
            return _globalScope != null && _globalScope.IsVariableDefined(name);
        }

        /// <summary>Returns the value of an existing global variable as a <see cref="ChowValue"/>.</summary>
        /// <param name="name">The global variable name. Must satisfy the global name rules.</param>
        /// <returns>The variable's value as a <see cref="ChowValue"/>.</returns>
        /// <exception cref="InvalidOperationException"><see cref="Execute"/> has not been called yet.</exception>
        /// <exception cref="GlobalAccessException">
        /// <paramref name="name"/> is invalid, reserved, or the variable does not exist.
        /// </exception>
        public ChowValue GetGlobal(string name)
        {
            ValidateGlobalExists(name, _globalScope);
            var varUnion = _globalScope.GetVariableValue(name);

            // Extracts the value from the TaggedUnion and converts it to a ChowValue to return
            return ApiConverter.ToChowValue(varUnion);
        }

        // TODO: Add to SetGlobal to declare the variable if it does not already exist
        /// <summary>
        /// Updates the value of an existing global variable. Unlike the indexer setter, this method requires
        /// the variable to already exist and will not create a new one.
        /// </summary>
        /// <param name="name">The global variable name. Must satisfy the global name rules.</param>
        /// <param name="value">The new value. Must not be <see langword="null"/>.</param>
        /// <exception cref="InvalidOperationException"><see cref="Execute"/> has not been called yet.</exception>
        /// <exception cref="GlobalAccessException">
        /// <paramref name="name"/> is invalid, reserved, or the variable does not exist, or
        /// <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        public void SetGlobal(string name, ChowValue value)
        {
            ValidateGlobalExists(name, _globalScope);

            if (value == null)
            {
                throw new GlobalAccessException(name, "Cannot assign null to a global variable");
            }

            if (_globalScope == null)
            {
                _globalScope = new Scope();
            }

            // Extracts the value from ChowValue and creates a new TaggedUnion containing the value & appropriate tag
            var varUnion = ApiConverter.ToTaggedUnion(value);

            // Method either assigns a new value to an existing variable, or declares & initializes a new variable
            _globalScope.AssignVariableValue(name, varUnion);
        }

        #endregion

        /// <summary>
        /// Registers the standard built-in functions into the global scope, making them available to Chow source code.
        /// Call this before <see cref="Execute"/> if your scripts depend on built-ins.
        /// </summary>
        /// <remarks>Any of these built-in functions can be overridden by reassigning them in the global scope.</remarks>
        public void ImportBuiltIns()
        {
            foreach ((string name, object obj) func in BuiltIns.GetFunctions())
            {
                this[func.name] = func.obj;
            }
        }

        /// <summary>
        /// Compiles and executes a string of Chow source code. The global scope persists across calls, so
        /// variables and functions defined in one call are available in subsequent calls.
        /// <see langword="null"/>, empty, and whitespace-only strings are accepted and treated as no-ops.
        /// </summary>
        /// <param name="sourceCode">The Chow source code to execute.</param>
        /// <exception cref="Exceptions.ScannerException">The source code contains a lexical error.</exception>
        /// <exception cref="Exceptions.ParserException">The source code contains a syntax error.</exception>
        /// <exception cref="Exceptions.ChowRuntimeException">A runtime error occurs during execution.</exception>
        public void Execute(string sourceCode)
        {
            // Source code that is null, empty, or whitespace is treated the same by the Scanner (it does a null check)
            // The rest of the pipeline does so as well to keep state consistent (e.g. _globalScope)
            var scanner = new Scanner(sourceCode);
            var tokens = scanner.ScanTokens();

            var parser = new Parser(tokens);
            var syntaxTreeRoot = parser.BuildTree();

            var compiler = new Compiler(syntaxTreeRoot);
            var chunk = compiler.CompileRoot();

            // Executes the chunk with the provided global scope, or if null, a new one
            var vm = new VirtualMachine(_globalScope, chunk);

            // TODO: Reprogram to have ChowModule always be the one to instantiate the global scope
            // The global scope will now contain any global variables & functions defined in the source code
            _globalScope = vm.EvaluateChunk();
        }

        /// <summary>
        /// Calls a Chow function that was defined during a previous <see cref="Execute"/> call or
        /// global scope assignment. Arguments are converted from host values automatically. Accepted
        /// argument types are <see langword="long"/>, <see langword="double"/>, <see langword="bool"/>,
        /// <see langword="string"/>, any <see cref="ChowValue"/> subclass, and <see cref="ChowObject"/>.
        /// </summary>
        /// <param name="functionName">
        /// The name of the global function to call. Must satisfy the global name rules.
        /// </param>
        /// <param name="arguments">Arguments to pass to the function.</param>
        /// <returns>
        /// The function's return value as a <see cref="ChowValue"/>. Returns <see cref="ChowValue.None"/> if
        /// the function returns <c>None</c> or has no return statement.
        /// </returns>
        /// <exception cref="InvalidOperationException"><see cref="Execute"/> has not been called yet.</exception>
        /// <exception cref="GlobalAccessException">
        /// <paramref name="functionName"/> is invalid, reserved, or not defined in the global scope.
        /// </exception>
        /// <exception cref="Exceptions.ChowRuntimeException">
        /// A runtime error occurs during the function call.
        /// </exception>
        public ChowValue CallFunction(string functionName, params object[] arguments)
        {
            ValidateGlobalExists(functionName, _globalScope);

            var vm = new VirtualMachine(_globalScope);
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

        static void ValidateGlobalExists(string name, Scope globalScope)
        {
            ValidateGlobalName(name);

            if (globalScope == null)
            {
                throw new InvalidOperationException($"{nameof(Execute)} was not called before attempting to access global variable '{name}'");
            }

            if (globalScope.IsVariableDefined(name))
            {
                return;
            }

            // If the name is valid, throw just that the name is not defined
            throw new GlobalAccessException(name, $"Global name '{name}' is not defined");
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
