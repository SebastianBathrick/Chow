using System;
namespace Chow.Exceptions
{
    sealed class ParserEx : Exception
    {
        public int LineNum { get; }

        public ParserEx(string msg, int lineNum) : base($"[line {lineNum}] Error: {msg}")
        {
            LineNum = lineNum;
        }
    }
}
