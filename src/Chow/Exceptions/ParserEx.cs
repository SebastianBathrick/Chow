using System;

namespace Chow.Interpreter.Exceptions
{
    sealed class ParserEx : Exception
    {
        int _lineNum;

        public int LineNum => _lineNum;

        public ParserEx(string msg, int lineNum) : base($"[line {lineNum}] Error: {msg}")
        {
            _lineNum = lineNum;
        }
    }
}
