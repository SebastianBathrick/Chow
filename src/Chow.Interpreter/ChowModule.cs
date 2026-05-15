using System.Collections.Generic;
using Chow.Interpreter.State.Scopes;
using Chow.Interpreter.State.Values;
using Chow.Interpreter.Exceptions;
using Chow.Interpreter.Tokens;
using Chow.Interpreter.Values;
using System;

namespace Chow.Interpreter
{
    /// <summary>
    /// The primary entry point for embedding the Chow interpreter. Manages a persistent global scope
    /// across multiple <see cref="Execute"/> calls so that variables and functions defined in one call
    /// are available in subsequent calls. The standard built-in functions are seeded into the global
    /// scope automatically at construction; hosts toggle or override them via the
    /// <c>SetBuiltIn*</c> methods.
    /// </summary>
    public class ChowModule
    {
        readonly Scope _globalScope = new Scope();
        readonly Dictionary<BuiltInType, object> _configuredBuiltIns;

        /// <summary>
        /// Creates a new module. The global scope is initialized eagerly and seeded with every
        /// standard built-in function. Hosts can immediately call <see cref="Execute"/>,
        /// <see cref="SetBuiltInActive"/>, or <see cref="SetBuiltInValue"/> without any prior setup.
        /// </summary>
        public ChowModule()
        {
            _configuredBuiltIns = new Dictionary<BuiltInType, object>();

            foreach (var type in BuiltIns.AllTypes)
            {
                var defaultImpl = BuiltIns.DefaultOf(type);
                _configuredBuiltIns[type] = defaultImpl;
                _globalScope.AssignVariableValue(BuiltIns.NameOf(type), ApiConverter.ToTaggedUnion(defaultImpl));
            }
        }

        #region Global Scope Access

        /// <summary>Gets or sets a global variable by name.</summary>
        /// <remarks>
        /// The getter returns the variable's value as a boxed primitive (<see langword="long"/>,
        /// <see langword="double"/>, <see langword="bool"/>, <see langword="string"/>) or a
        /// <see cref="ChowValue"/> subclass for composite types (list, dict, object, function).
        /// <para>
        /// The setter creates the variable if it does not already exist. Accepted value types are
        /// <see langword="long"/>, <see langword="double"/>, <see langword="bool"/>,
        /// <see langword="string"/>, any <see cref="ChowValue"/> subclass, and <see cref="ChowObject"/>.
        /// </para>
        /// </remarks>
        /// <param name="name">The global variable name. Must satisfy the global name rules.</param>
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

                var varUnion = ApiConverter.ToTaggedUnion(value);

                // Method either assigns a new value to an existing variable, or declares & initializes a new variable
                _globalScope.AssignVariableValue(name, varUnion);
            }
        }

        /// <summary>
        /// Returns <see langword="true"/> if the named variable exists in the global scope.
        /// </summary>
        /// <param name="name">The global variable name to check.</param>
        public bool ContainsGlobal(string name)
        {
            return _globalScope.IsVariableDefined(name);
        }

        /// <summary>Returns the value of an existing global variable as a <see cref="ChowValue"/>.</summary>
        /// <param name="name">The global variable name. Must satisfy the global name rules.</param>
        /// <returns>The variable's value as a <see cref="ChowValue"/>.</returns>
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

            // Extracts the value from ChowValue and creates a new TaggedUnion containing the value & appropriate tag
            var varUnion = ApiConverter.ToTaggedUnion(value);

            // Method either assigns a new value to an existing variable, or declares & initializes a new variable
            _globalScope.AssignVariableValue(name, varUnion);
        }

        #endregion

        #region Built-In Functions

        /// <summary>
        /// Controls whether a built-in is visible to Chow source code. Disabling removes the binding
        /// from the global scope so a reference to it raises a <c>NameError</c>; re-enabling reinstalls
        /// the currently *configured* value (the default, or whatever was most recently passed to
        /// <see cref="SetBuiltInValue"/>). Idempotent.
        /// </summary>
        /// <param name="type">The built-in to toggle.</param>
        /// <param name="active"><c>true</c> to make the built-in callable from Chow code; <c>false</c> to hide it.</param>
        public void SetBuiltInActive(BuiltInType type, bool active)
        {
            var name = BuiltIns.NameOf(type);

            if (active)
            {
                _globalScope.AssignVariableValue(name, ApiConverter.ToTaggedUnion(_configuredBuiltIns[type]));
            }
            else
            {
                _globalScope.RemoveVariable(name);
            }
        }

        /// <summary>
        /// Overrides the implementation of a built-in. The new value is retained even across
        /// <see cref="SetBuiltInActive"/> toggles, so a host that customizes a built-in does not have
        /// to re-apply the override after a disable/enable cycle. If the built-in is currently active,
        /// the new value takes effect immediately; if inactive, the override is held until the next
        /// <see cref="SetBuiltInActive"/>(<paramref name="type"/>, <c>true</c>) call.
        /// </summary>
        /// <param name="type">The built-in to override.</param>
        /// <param name="value">
        /// The new implementation. Accepted forms match those of the global-variable setter
        /// (delegates such as <see cref="Func{T, TResult}"/>, <see cref="ChowValue"/> subclasses,
        /// <see cref="ChowObject"/>, and primitive types).
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.
        /// Use <see cref="SetBuiltInActive"/> with <c>active: false</c> to remove a built-in.</exception>
        public void SetBuiltInValue(BuiltInType type, object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            _configuredBuiltIns[type] = value;

            var name = BuiltIns.NameOf(type);

            if (_globalScope.IsVariableDefined(name))
            {
                _globalScope.AssignVariableValue(name, ApiConverter.ToTaggedUnion(value));
            }
        }

        /// <summary>
        /// Returns <see langword="true"/> if the built-in is currently callable from Chow source code
        /// (i.e., its name is bound in the global scope).
        /// </summary>
        /// <param name="type">The built-in to query.</param>
        public bool IsBuiltInActive(BuiltInType type)
        {
            return _globalScope.IsVariableDefined(BuiltIns.NameOf(type));
        }

        #endregion

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

            var vm = new VirtualMachine(_globalScope, chunk);

            // The VM mutates _globalScope in place; the returned reference is the same object.
            vm.EvaluateChunk();
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
