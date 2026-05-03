using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Syntax.Trees
{
    internal class SyntaxTreeRoot : Node
    {
        Node _topLvlBlock;

        public Node TopLevelBlock => _topLvlBlock;

        public SyntaxTreeRoot(Node topLvlBlock, int lineNumber)
            : base(lineNumber)
        {
            if (topLvlBlock == null)
            {
                throw new ArgumentNullException(nameof(topLvlBlock));
            }

            _topLvlBlock = topLvlBlock;
        }

        public override string ToString()
        {
            return $"SyntaxTreeRoot line={LineNumber}\n{_topLvlBlock}";
        }
    }
}
