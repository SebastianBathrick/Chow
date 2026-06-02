using System;
using Chow.Exceptions;
using Chow.StandardLibrary;
using Chow.State;
namespace Chow
{
    /// <summary>
    /// Represents a managed global scope that maintains the state of global variables and functions.
    /// </summary>
    public sealed class ChowState
    {
        // The name is for later features, such as import statements
        readonly Scope _globalScope;
        bool _needsBuiltInsImport;
        
        #region Properties

        /// <summary>This module's name.</summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the value of a global variable or function with the specified name.
        /// </summary>
        /// <param name="name">Name of the variable/function to set/get.</param>
        /// <exception cref="GlobalAccessException">Thrown by get when <paramref name="name"/> is
        /// not bound to an existing variable.</exception>
        /// <remarks>If on set, <paramref name="name"/> is not bound to an existing variable, a new
        /// variable will be declared, bound to <paramref name="name"/>, and initialized to the
        /// specified value.</remarks>
        public object this[string name]
        {
            get
            {
                if (_needsBuiltInsImport)
                {
                    // Variable being retrieved could be or rely on a built-in function
                    ImportBuiltIns();
                }
                
                return _globalScope.ContainsVariable(name) 
                    ? _globalScope.GetVariableValue(name).AsType<object>() 
                    : throw new GlobalAccessException(name, $"name '{name}' is not defined");

            }
            set
            {
                if (_needsBuiltInsImport)
                {
                    // The client could be trying to override a built-in function
                    ImportBuiltIns();
                }
                
                var chowValue = new ChowValue(value);

                if (!IsValidChowName(name))
                {
                    throw new GlobalAccessException(name,
                        $"'{name}' does not abide by the rules for variable names");
                }

                _globalScope.AssignVariableValue(name, chowValue);

            }
        }

        #endregion

        /// <summary>Initializes a ChowState with optional built-in functions.</summary>
        /// <param name="name">This module's name.</param>
        /// <param name="useBuiltIns">Whether built-in functions will be automatically added
        /// to the global scope.</param>
        public ChowState(string name = nameof(ChowState), bool useBuiltIns = true)
        {
            Name = name ?? nameof(ChowState);

            _globalScope = new Scope();

            // Use a flag to avoid importing built-ins during initialization, because what
            // BuiltInFunctions executes should be considered unknown to ChowState
            _needsBuiltInsImport = useBuiltIns;
        }


        /// <summary>
        /// Compiles and interprets Chow source code in a <see langword="string"/>.
        /// </summary>
        /// <param name="sourceCode">String containing Chow source code, whitespace, or null.</param>
        /// <returns><see cref="ChowValue.None"/>, or the result of the last expression statement
        /// interpreted (if one did).</returns>
        public ChowValue Execute(string sourceCode)
        {
            if (_needsBuiltInsImport)
            {
                ImportBuiltIns();
            }
            
            return ChowEngine.ExecuteModuleCode(sourceCode, _globalScope);
        }
        
        /// <summary>
        /// Declares the standard built-in functions (e.g. <c>print</c>, <c>len</c>, <c>range</c>)
        /// in this module's global scope.
        /// </summary>
        public void ImportBuiltIns()
        {
            // BuiltInFunctions does not create its own Module like you may expect. Instead,
            // ChowState wraps invocable C# objects into ChowValues, and binds them to a name
            // manually to avoid a circular dependency.
            var namedInvocableObjects = BuiltInFunctions.NamedInvocableObjects;

            foreach (var namedInvocable in namedInvocableObjects)
            {
                var chowValue = new ChowValue(namedInvocable.callableObject);
                
                // It's assumed that the name and value are valid because they're defined inside
                // the BuiltInFunctions static class. Thus, this does not require the same level
                // of variable validation as something like the indexer.
                _globalScope.AssignVariableValue(namedInvocable.name, chowValue);
            }
            
            // Clear the flag — built-ins have now been imported at least once.
            _needsBuiltInsImport = false;
        }
        
        #region Global Scope API Methods

        /// <summary>Invokes Chow function or an interop function with the provided arguments.</summary>
        /// <param name="functionName">The name of the variable the target function was assigned to.</param>
        /// <param name="args">Boxed host language values to pass to the function as arguments.</param>
        /// <returns>If the target was non-void function, then this method will return target function's
        /// returned <see cref="ChowValue"/>. </returns>
        /// <exception cref="GlobalAccessException">Thrown when <paramref name="functionName"/> is
        /// not defined in this module's global scope.</exception>
        public ChowValue InvokeGlobal(string functionName, params object[] args)
        {
            // IsGlobal imports built-ins if needed
            if (!IsGlobal(functionName))
            {
                throw new GlobalAccessException(
                    functionName, $"'{functionName}' is not defined in the global scope");
            }

            var convertedArgs = ConvertToChowValues(args);
            return ChowEngine.InvokeChowFunction(_globalScope, functionName, convertedArgs);
        }

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="name"/> is defined in this module's
        /// global scope.
        /// </summary>
        public bool IsGlobal(string name)
        {
            if (_needsBuiltInsImport)
            {
                ImportBuiltIns();
            }
            
            return _globalScope.ContainsVariable(name);
        }

        #endregion

        #region Global Scope API Helper Methods

        static ChowValue[] ConvertToChowValues(object[] args)
        {
            if (args == null || args.Length == 0)
            {
                // TODO: Consider returning null instead of an empty array
                return Array.Empty<ChowValue>();
            }

            var result = new ChowValue[args.Length];

            for (var i = 0; i < args.Length; i++)
            {
                result[i] = new ChowValue(args[i]);
            }

            return result;
        }

        // Returns true if the name follows Python variable name rules.
        static bool IsValidChowName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            var first = name[0];
            if (!IsLetterChar(first) && first != '_')
            {
                return false;
            }

            for (var i = 1; i < name.Length; i++)
            {
                var c = name[i];
                if (!IsLetterChar(c) && !IsDigitChar(c) && c != '_')
                {
                    return false;
                }
            }

            return true;
        }

        // Avoids char.IsLetter(char) because it is slower due to large Unicode checks
        static bool IsLetterChar(char c)
        {
            return c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z';
        }

        
        // Consult the documentation for IsLetterChar, because this function exists for a similar reason
        static bool IsDigitChar(char c)
        {
            return c >= '0' && c <= '9';
        }

        #endregion
    }
}
