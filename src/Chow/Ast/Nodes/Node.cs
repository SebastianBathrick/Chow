namespace Chow.Ast
{
    /// <summary>Represents a single node in an abstract syntax tree (AST).</summary>
    abstract class Node
    {
        /// <summary>The line of source code that prompted the creation of this node.</summary>
        public int LineNumber { get; }

        protected Node(int line)
        {
            LineNumber = line;
        }
    }
}
