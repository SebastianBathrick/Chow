using System.Collections.Generic;

namespace Chow.Syntax
{
    /// <summary>
    /// Represents a class definition. The body is split by member kind at parse time rather than
    /// kept as a block, because the two are compiled differently: methods become their own bytecode
    /// chunks, while class variables are evaluated in the enclosing scope at declaration time.
    /// </summary>
    sealed class ClassNode : Node
    {
        public string ClassName { get; }

        /// <summary>The methods declared in the class body (empty when none).</summary>
        public List<FunctionNode> Methods { get; }

        /// <summary>The class-level variables declared in the class body (empty when none).</summary>
        public List<AssignStatementNode> ClassVariables { get; }

        /// <summary>The scope the class name is bound in, stamped during semantic analysis.</summary>
        public ScopeType Resolution { get; set; }

        public ClassNode(
            string className,
            List<FunctionNode> methods,
            List<AssignStatementNode> classVariables,
            int line) : base(line)
        {
            ClassName = className;
            Methods = methods;
            ClassVariables = classVariables;
        }
    }
}
