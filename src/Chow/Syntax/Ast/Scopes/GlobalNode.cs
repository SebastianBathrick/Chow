using System.Collections.Generic;

namespace Chow.Syntax
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
