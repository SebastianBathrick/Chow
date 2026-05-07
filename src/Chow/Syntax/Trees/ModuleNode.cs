using System.Collections.Generic;

namespace Chow.Interpreter.Syntax.Trees
{
    internal class ModuleNode : BlockNode
    {
        const int LINE_NUMBER = 1;
        public ModuleNode(List<Node> statements) : base(statements, LINE_NUMBER)
        {
        }
    }
}
