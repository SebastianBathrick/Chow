using System.Collections.Generic;

namespace Chow.Syntax
{
    /// <summary>Represents a function definition and body.</summary>
    sealed class FunctionNode : Node
    {
        public string Name { get; }

        /// <summary>List containing each of the function's parameters (empty when none).</summary>
        public List<Node> Params { get; }

        /// <summary>The function's body.</summary>
        public Node Block { get; }

        /// <summary>The scope where the function was defined in the source code.</summary>
        public ScopeType Resolution { get; set; }

        public FunctionNode(string name, List<Node> paramList, Node block, int line) : base(line)
        {
            Name = name;
            Params = paramList;
            Block = block;
        }

    }
}
