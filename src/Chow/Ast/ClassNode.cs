using System.Collections.Generic;

namespace Chow.Syntax
{
    class ClassNode : Node
    {
        public string ClassName { get; }
        
        public Node BlockNode { get; }

        public ClassNode(string className, Node blockNode, int line) : base(line)
        {
            ClassName = className;
            BlockNode = blockNode;
        }
    }
}
