namespace Chow.Interpreter.Bytecode
{
    /// <summary>
    /// Represents an <see cref="Instruction"/>'s operation code, which is mapped to the logic inside the
    /// <see cref="Interpreter.VirtualMachine"/> class.
    /// </summary>
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
        Not,
        JumpIfFalseOrPop,
        JumpIfTrueOrPop,
        PushConstant,
        VariableAssignOrDeclare,
        VariablePushValue,
        ReturnValue,
        PopExpressionStatementResult,
        JumpIfFalse,
        JumpPastElseBranches,
        JumpToLoopStart,
        IncScopeDepth,
        DecScopeDepth,
        Call,
        CreateClosureFromTemplate,
        CreateInternalList,
        Subscript,
        SubscriptSlice,
        SubscriptSet,
        GetVariableAttribute,
        SetVariableAttribute,
        CreateInternalDict,
        BinaryOr,
        In,
        NotIn,
    }
}