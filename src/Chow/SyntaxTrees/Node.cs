namespace Chow.Interpreter.SyntaxTrees
{
    internal abstract class Node
    {
        readonly int _lineNumber;

        public int LineNum => _lineNumber;

        protected Node(int line)
        {
            _lineNumber = line;
        }

        public abstract override string ToString();
    }
}
