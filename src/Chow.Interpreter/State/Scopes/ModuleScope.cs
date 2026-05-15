namespace Chow.Interpreter.State.Scopes
{
    /// <summary>
    /// Module-level scope. Lives on <see cref="ChowModule" /> and persists across every <c>Execute</c> call so REPL-style state
    /// carries between inputs. Acts as the G (Global) terminator of LEGB lookup chains and never has a parent.
    /// </summary>
    sealed class ModuleScope : Scope
    {
    }
}
