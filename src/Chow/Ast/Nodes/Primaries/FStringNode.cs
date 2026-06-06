using System.Collections.Generic;
namespace Chow.SyntaxTrees.Literals
{
    // Format specs (e.g. {x:.2f}) are not supported in v1; a colon inside a slot will raise a ParserException.
    sealed class FStringNode : Node
    {
        public IReadOnlyList<string> StringParts { get; }

        public IReadOnlyList<Node> ExpressionParts { get; }

        public FStringNode(IReadOnlyList<string> stringParts, IReadOnlyList<Node> expressionParts, int line)
            : base(line)
        {
            StringParts = stringParts;
            ExpressionParts = expressionParts;
        }
    }
}
