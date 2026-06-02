using System;
namespace Chow.Exceptions
{
    /// <summary>
    /// Thrown by the <see cref="ChowState"/> host API when a global variable access is invalid.
    /// </summary>
    public class GlobalAccessException : Exception
    {
        const string EXCEPTION_ALIAS = "Global Access Error";

        /// <summary>Gets the variable name that triggered the exception.</summary>
        public string Name { get; }

        // TODO: Change so that the client doesn't need to write the whole message each time this throws
        /// <summary>Initialises a new <see cref="GlobalAccessException"/> for the given variable name.</summary>
        /// <param name="name">The variable name that caused the error.</param>
        /// <param name="msg">A message describing the specific violation.</param>
        public GlobalAccessException(string name, string msg) : base($"{EXCEPTION_ALIAS}: {msg}")
        {
            Name = name;
        }
    }
}
