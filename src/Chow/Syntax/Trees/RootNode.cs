using System;

namespace Chow.Interpreter.Syntax.Trees
{
    internal class RootNode : Node
    {
        Node module;

        public Node Module => module;

        public RootNode(Node module, int line) : base(line)
        {
            this.module = module;
        }

        public override string ToString()
        {
            return $"SyntaxTreeRoot line={LineNum}\n{module}";
        }
    }
}
