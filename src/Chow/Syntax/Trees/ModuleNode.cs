using System;
using System.Collections.Generic;
using System.Text;

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
