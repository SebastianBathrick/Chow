namespace Chow.Syntax.Trees
{
    internal class EmptyNode : Node
    {
        public EmptyNode()
            : base(1)
        {
        }

        public override string ToString()
        {
            return "Empty";
        }
    }
}
