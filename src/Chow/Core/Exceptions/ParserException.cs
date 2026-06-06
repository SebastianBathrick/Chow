using System;
namespace Chow.Exceptions
{
    sealed class ParserException : Exception
    {
        public int LineNum { get; }

        public ParserException(string msg, int lineNum) : base($"[line {lineNum}] Error: {msg}")
        {
            LineNum = lineNum;
        }
    }
}
