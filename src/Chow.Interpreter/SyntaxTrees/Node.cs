namespace Chow.Interpreter.SyntaxTrees
{
    abstract class Node
    {
        protected Node(int line)
        {
            LineNumber = line;
        }
        public int LineNumber { get; }

        public abstract override string ToString();
    }
}
