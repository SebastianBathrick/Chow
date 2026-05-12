using System;

namespace Chow.Interpreter.Exceptions
{
    public class GlobalAccessException : Exception
    {
        const string EXCEPTION_ALIAS = "Global Access Error";

        string _name;

        public string Name => _name;

        public GlobalAccessException(string name, string msg) : base($"{EXCEPTION_ALIAS}: {msg}")
        {
            _name = name;
        }
    }
}
