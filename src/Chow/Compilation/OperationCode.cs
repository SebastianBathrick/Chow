namespace Chow.Interpreter.Compilation
{
    enum OperationCode
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulus,
        Exponentiate,
        FloorDivide,
        Negate,
        PushConstant,
        AssignOrDeclareVariable,
        PushVariableValue,
        ReturnValue,
        PopExprStmntResult
    }
}