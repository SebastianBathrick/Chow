using System;
namespace Chow.Interpreter.Exceptions
{
    /// <summary>
    /// Thrown by the <see cref="Chow.Interpreter.ChowModule"/> host API when a global variable access is invalid.
    /// Possible causes: the name is <see langword="null"/>, empty, or whitespace; the name is a reserved keyword;
    /// the name does not satisfy identifier rules; the variable does not exist; or a <see langword="null"/> value
    /// was passed to <see cref="Chow.Interpreter.ChowModule.SetGlobal"/>.
    /// </summary>
    public class GlobalAccessException : Exception
    {
        const string EXCEPTION_ALIAS = "Global Access Error";

        /// <summary>Gets the variable name that triggered the exception.</summary>
        public string Name { get; }

        /// <summary>Initialises a new <see cref="GlobalAccessException"/> for the given variable name.</summary>
        /// <param name="name">The variable name that caused the error.</param>
        /// <param name="msg">A message describing the specific violation.</param>
        public GlobalAccessException(string name, string msg) : base($"{EXCEPTION_ALIAS}: {msg}")
        {
            Name = name;
        }
    }
}
