namespace Chow.Interpreter.SyntaxTrees
{
    abstract class Node
    {
        public int LineNumber { get; }

        protected Node(int line)
        {
            LineNumber = line;
        }

        public abstract override string ToString();
    }
}
