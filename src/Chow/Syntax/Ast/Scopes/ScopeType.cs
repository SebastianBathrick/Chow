namespace Chow.Syntax
{
    /// <summary>Represents the scope type of individual variables and functions.</summary>
    /// <remarks>
    /// Certain <see cref="Node"/> objects require a scope type, which is defined during semantic
    /// analysis. The compiler then selects an instruction based on the node’s scope type.
    /// </remarks>
    enum ScopeType
    {
        Local,
        Global,
        NonLocal
    }
}
