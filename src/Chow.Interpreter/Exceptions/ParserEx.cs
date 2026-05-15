using System;
namespace Chow.Interpreter.Exceptions
{
    sealed class ParserEx : Exception
    {
        public ParserEx(string msg, int lineNum) : base($"[line {lineNum}] Error: {msg}")
        {
            LineNum = lineNum;
        }
        public int LineNum { get; }
    }
}
