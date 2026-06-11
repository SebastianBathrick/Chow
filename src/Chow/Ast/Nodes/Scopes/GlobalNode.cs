using System.Collections.Generic;

namespace Chow.Ast
{
    sealed class GlobalNode : Node
    {
        public List<string> VariableNames { get; }

        public GlobalNode(List<string> varNames, int line) : base(line)
        {
            VariableNames = varNames;
        }
    }
}
