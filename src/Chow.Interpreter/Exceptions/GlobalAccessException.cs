using System;
namespace Chow.Interpreter.Exceptions
{
    public class GlobalAccessException : Exception
    {
        const string EXCEPTION_ALIAS = "Global Access Error";

        public GlobalAccessException(string name, string msg) : base($"{EXCEPTION_ALIAS}: {msg}")
        {
            Name = name;
        }

        public string Name { get; }
    }
}
