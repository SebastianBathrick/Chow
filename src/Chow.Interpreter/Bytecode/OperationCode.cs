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
        AssignOrDeclareVariable,
        PushVariableValue,
        ReturnValue,
        PopExprStmntResult,
        JumpIfFalse,
        JumpPastBranches,
        Loop,
        IncScopeDepth,
        DecScopeDepth,
        Call,
        MakeClosure,
        BuildList,
        Subscript,
        SubscriptSlice,
        SubscriptSet,
        GetAttr,
        SetAttr,
        BuildDict,
        BinaryOr,
        In,
        NotIn,
    }
}