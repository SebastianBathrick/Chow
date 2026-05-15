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
        PopAndAssignToVariable,
        PushVariableValue,
        PopAndAssignToGlobal,
        PushGlobalValue,
        PopAndAssignToNonlocal,
        PushNonlocalValue,
        PushReturnValue,
        PopExpressionStatementResult,
        JumpIfFalse,
        JumpPastElseBranches,
        JumpToLoopStart,
        CallFunction,
        PushNewClosureFromTemplate,
        PushNewInternalList,
        Subscript,
        SubscriptSlice,
        SubscriptSet,
        GetObjectAttribute,
        SetInteropObjectAttribute,
        PushNewInternalDict,
        BinaryOr,
        In,
        NotIn,
        GetIterator,
        ForIterNextOrJump,
        Pop,
    }
}