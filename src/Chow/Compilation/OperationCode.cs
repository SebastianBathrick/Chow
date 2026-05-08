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
        Equal,
        NotEqual,
        Less,
        Greater,
        LessEqual,
        GreaterEqual,
        And,
        Or,
        Not,
        PushConstant,
        AssignOrDeclareVariable,
        PushVariableValue,
        ReturnValue,
        PopExprStmntResult
    }
}