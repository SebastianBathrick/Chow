using Chow.Interpreter.Exceptions;
using Chow.Interpreter.State;
using System;
using Chow.Interpreter.StandardLibrary;
namespace Chow.Interpreter
{
    /// <summary>
    /// Represents a managed global scope where global variables are declared via an instance’s
    /// indexer or using source code variable declarations (or function definitions) executed using
    /// the instance’s public methods. 
    /// </summary>
    public sealed class ChowModule
    {
        // The name is for later features, such as import statements, but their implementation has
        // not been planned in detail.
        readonly string _name;
        readonly Scope _globalScope;

        // If true, built-ins are required for this module but have not been imported. If false,
        // built-ins are not required for this module or have already been imported at least once.
        bool _needsBuiltInsImport;
        
        #region Properties

        /// <summary>Read-only name this instance was initialized with.</summary>
        public string Name => _name;

        /// <summary>
        /// <para>
        /// Returns the value of, or assigns a value to, a variable bound to a
        /// <see langword="string"/> name.
        /// </para>
        /// <para>
        /// When setting a variable value, if the variable is undefined, a new variable is declared
        /// and initialized to the provided value.
        /// </para>
        /// </summary>
        /// <param name="name">Name of the variable or function to set/get.</param>
        /// <exception cref="GlobalAccessException">Thrown when <paramref name="name"/> is
        /// not defined in this module's global scope.</exception>
        public object this[string name]
        {
            get
            {
                if (_needsBuiltInsImport)
                {
                    // The global variable being retrieved might be a built-in function not yet imported
                    ImportBuiltIns();
                }
                
                if (_globalScope.ContainsVariable(name))
                {
                    return _globalScope.GetVariableValue(name).AsType<object>();
                }

                throw new GlobalAccessException(name, $"name '{name}' is not defined");
            }
            set
            {
                if (_needsBuiltInsImport)
                {
                    // The client could be trying to override a built-in function
                    ImportBuiltIns();
                }
                
                var chowValue = new ChowValue(value);

                if (IsValidChowName(name))
                {
                    _globalScope.AssignVariableValue(name, chowValue);
                    return;
                }
                
                throw new GlobalAccessException(name, 
                    $"'{name}' does not abide by the rules for variable names");
            }
        }

        #endregion

        /// <summary>Initializes a ChowModule with no global variables or function definitions.</summary>
        public ChowModule(string name = nameof(ChowModule), bool useBuiltInFunctions = true)
        {
            if (name != null)
            {
                _name = name;
            }
            else
            {
                _name = nameof(ChowModule);
            }

            _globalScope = new Scope();

            // Use a flag to avoid importing built-ins during initialization, because what
            // BuiltInFunctions executes should be considered unknown to ChowModule
            _needsBuiltInsImport = useBuiltInFunctions;
        }


        /// <summary>Compiles and interprets Chow source code contained in a <see langword="string"/>.</summary>
        /// <param name="sourceCode">String containing Chow source code, whitespace, or null.</param>
        /// <returns><see cref="ChowValue.None"/>, or the result of the last expression statement
        /// interpreted, if there was one defined in <paramref name="sourceCode"/>, and it is not null.</returns>
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
        /// in this module's global scope so they can be called from source code or by calling
        /// this instance's <see cref="InvokeGlobal(string, object[])"/>.
        /// </summary>
        public void ImportBuiltIns()
        {
            // BuiltInFunctions does not create its own Module like you may expect. Instead,
            // ChowModule wraps invocable C# objects into ChowValues, and binds them to a name
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
