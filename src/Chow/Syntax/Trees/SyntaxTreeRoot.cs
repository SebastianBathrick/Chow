using System;

namespace Chow.Interpreter.Syntax.Trees
{
    internal class SyntaxTreeRoot : Node
    {
        Node _moduleNode;

        public Node ModuleNode => _moduleNode;

        public SyntaxTreeRoot(Node moduleNode, int lineNumber)
            : base(lineNumber)
        {
            if (moduleNode == null)
            {
                throw new ArgumentNullException(nameof(moduleNode));
            }

            _moduleNode = moduleNode;
        }

        public override string ToString()
        {
            return $"SyntaxTreeRoot line={LineNumber}\n{_moduleNode}";
        }
    }
}
