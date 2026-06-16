using System.Collections.Generic;

namespace Chow.Syntax
{
    // TODO: Add colons to fstrings (e.g. {x:.2f}).
    sealed class FStringNode : Node
    {
        public List<string> StringParts { get; }

        public List<Node> ExpressionParts { get; }

        public FStringNode(List<string> strParts, List<Node> exprParts, int line) : base(line)
        {
            StringParts = strParts;
            ExpressionParts = exprParts;
        }
    }
}
