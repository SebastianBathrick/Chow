namespace Chow.Syntax
{
    /// <summary>
    /// Represents a <c>pass</c> statement. Carries no state and emits no bytecode; it exists so a
    /// block that requires a statement can be written without performing any work.
    /// </summary>
    sealed class PassStatementNode : Node
    {
        public PassStatementNode(int line) : base(line)
        {
        }
    }
}
