using Chow.Interpreter.Exceptions;
using Chow.Interpreter.State;
using Chow.Interpreter.Values;
using System;
using System.Collections.Generic;
namespace Chow.Interpreter
{
    /// <summary>
    /// Represents a managed global scope where global variables are declared via an instance’s
    /// indexer or using source code variable declarations (or function definitions) executed using
    /// the instance’s public methods. 
    /// </summary>
    public sealed class ChowModule
    {
        Scope _globalScope;
        readonly List<string> _prevSourceCode;

        #region Properties
        /// <summary>
        /// <para>
        /// Returns the value of, or assigns a value to, a variable bound to a
        /// <see langword="string"/> name.
        /// </para>
        /// <para>
        /// When getting a variable value, if it is undefined, then <see langword="null"/> will be
        /// returned.
        /// </para>
        /// <para>
        /// When setting a variable value, if the variable is undefined, a new variable is declared
        /// and initialized to the provided value.
        /// </para>
        /// </summary>
        /// <param name="name">Name of the variable or function to set/get.</param>
        public object this[string name]
        {
            get
            {
                if (_globalScope.ContainsVariable(name))
                {
                    return _globalScope.GetVariableValue(name).AsType<object>();
                }

                return null;
            }
            set
            {
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
        public ChowModule()
        {
            _globalScope = new Scope();
            _prevSourceCode = new List<string>();
        }


        /// <summary>  Compiles and interprets Chow source code contained in a <see langword="string"/>. </summary>
        /// <param name="sourceCode">String containing Chow source code, whitespace, or null.</param>
        /// <returns><see cref="ChowValue.None"/>, or the result of the last expression statement
        /// interpreted, if there was one defined in <paramref name="sourceCode"/>, and it is not null.</returns>
        public ChowValue Execute(string sourceCode)
        {
            var returnVal = ChowEngine.ExecuteModuleCode(sourceCode, _globalScope);
            
            // Only add the source code AFTER it has successfully executed. This avoids executing
            // source code that throws exceptions when using the public Import(ChowModule).
            _prevSourceCode.Add(sourceCode);
            return returnVal;
        }

        /// <summary>
        /// <para>
        /// Imports another <see cref="ChowModule"/>'s globals into this module by merging its
        /// global scope into this one.
        /// </para>
        /// <para>
        /// When <paramref name="executeTopLevel"/> is <see langword="true"/>, each source code
        /// string previously executed on <paramref name="moduleToImport"/> is re-executed against
        /// this module first, so any top-level statements (beyond variable declarations and
        /// function definitions) run in this module's context.
        /// </para>
        /// <para>
        /// After execution, the imported module's current global scope is applied on top, so any
        /// changes made to it via its indexer (after its last <see cref="Execute(string)"/> call)
        /// are also reflected.
        /// </para>
        /// </summary>
        /// <param name="moduleToImport">The module whose source-code history and global scope are
        /// imported into this module.</param>
        /// <param name="executeTopLevel">When <see langword="false"/> (default), only the imported
        /// module's current global scope state is applied, without re-running its source. When
        /// <see langword="true"/>, re-executes every source code string previously executed on
        /// <paramref name="moduleToImport"/> in this module first.</param>
        /// <returns><see cref="ChowValue.None"/>, or the result of the final expression statement
        /// produced by re-executing the imported module's source code when
        /// <paramref name="executeTopLevel"/> is <see langword="true"/>.</returns>
        public ChowValue Import(ChowModule moduleToImport, bool executeTopLevel = false)
        {
            ChowValue returnValue = ChowValue.None;

            // If the client does not only want to import the module's current state
            if (executeTopLevel)
            {
                // Execute each previously executed Chow source code, in case there were top-level
                // statements other than variable declarations and function definitions.
                foreach (var sourceCode in moduleToImport._prevSourceCode)
                {
                    returnValue = Execute(sourceCode);
                }
            }

            // NOTE: If a variable value was declared/reassigned using the indexer before the
            // last ChowModule.Execute(string) call, those intermediate changes to the scope
            // will not apply. Thus, source code execution might behave different under such
            // conditions.
            
            // Apply any changes made to the variables using the imported module's indexer.
            _globalScope += moduleToImport._globalScope;
            return returnValue;
        }

        #region Global Scope API Methods

        /// <summary>
        /// Invokes Chow function or an interop function with the provided arguments.
        /// </summary>
        /// <param name="functionName">The name of the variable the target function was assigned to.</param>
        /// <param name="args">Boxed host language values to pass to the function as arguments.</param>
        /// <returns>If the target was non-void function, then this method will return target function's
        /// returned <see cref="ChowValue"/>. </returns>
        /// <exception cref="GlobalAccessException"></exception>
        public ChowValue InvokeGlobal(string functionName, params object[] args)
        {
            if (IsGlobal(functionName))
            {
                var convertedArgs = ConvertToChowValues(args);
                return ChowEngine.InvokeChowFunction(_globalScope, functionName, convertedArgs);
            }

            throw new GlobalAccessException(functionName, $"'{functionName}' is not defined in the global scope");
        }

        /// <summary>
        /// <para>
        /// Returns <see langword="true"/> if <paramref name="name"/> is defined in the module's
        /// global scope.
        /// </para>
        /// <para>
        /// If the name does not follow the rules for variable names in Chow. As 
        /// </para>
        /// </summary>
        /// <exception cref="GlobalAccessException">Thrown when <paramref name="name"/> is null.</exception>
        public bool IsGlobal(string name)
        {
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

        // Returns true if essentially the name abides by the same name rules as Python.
        static bool IsValidChowName(string name)
        {
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
