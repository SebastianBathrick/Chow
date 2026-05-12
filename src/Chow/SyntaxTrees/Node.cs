namespace Chow.Interpreter.SyntaxTrees
{
    abstract class Node
    {
        readonly int _lineNumber;

        public int LineNumber => _lineNumber;

        protected Node(int line)
        {
            _lineNumber = line;
        }

        public abstract override string ToString();
    }
}
