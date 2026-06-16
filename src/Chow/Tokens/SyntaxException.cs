using System;

namespace Chow
{
    sealed class SyntaxException : Exception
    {
        public int LineNumber { get; }

        public SyntaxException(string expected, int line)
            : base($"SyntaxError: expected '{expected}'")
        {
            LineNumber = line;
        }
    }
}
