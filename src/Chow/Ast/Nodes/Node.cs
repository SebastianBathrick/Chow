namespace Chow.Ast
{
    abstract class Node
    {
        public int LineNumber { get; }

        protected Node(int line)
        {
            LineNumber = line;
        }
    }
}
