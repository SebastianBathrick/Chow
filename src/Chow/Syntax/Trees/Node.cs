using System;

namespace Chow.Interpreter.Syntax.Trees
{
    internal abstract class Node
    {
        readonly int _lineNumber;

        public int LineNumber => _lineNumber;

        protected Node(int lineNumber)
        {
            if (lineNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(lineNumber));

            _lineNumber = lineNumber;
        }

        public abstract override string ToString();
    }
}
