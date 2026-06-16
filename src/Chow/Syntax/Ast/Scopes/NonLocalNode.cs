using System.Collections.Generic;

namespace Chow.Syntax
{
    sealed class NonLocalNode : Node
    {
        public List<string> VariableNames { get; }

        public NonLocalNode(List<string> varNames, int line) : base(line)
        {
            VariableNames = varNames;
        }
    }
}
