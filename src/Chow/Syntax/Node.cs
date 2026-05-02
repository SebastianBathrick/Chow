using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Syntax
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
