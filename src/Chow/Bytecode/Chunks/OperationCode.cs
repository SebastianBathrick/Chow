using Chow.VM;
namespace Chow.Bytecode
{
    /// <summary>
    /// Represents an <see cref="Instruction"/>'s operation code, which is mapped to the logic inside the
    /// <see cref="Processor"/> class.
    /// </summary>
    enum OperationCode
    {
        BinaryAdd,
        BinarySubtract,
        BinaryMultiply,
        BinaryDivide,
        BinaryModulus,
        BinaryPow,
        BinaryFloor,
        UnaryNegate,
        BinaryEqual,
        BinaryNotEqual,
        BinaryLess,
        BinaryGreater,
        BinaryLessEqual,
        BinaryGreaterEqual,
        UnaryNot,
        JumpIfFalseOrPop,
        JumpIfTrueOrPop,
        PushConstantValue,
        AssignVariable,
        PushVariableValue,
        AssignGlobal,
        PushGlobalValue,
        AssignNonLocal,
        PushNonLocalValue,
        PushReturnValue,
        PopExpressionStatementResult,
        JumpIfFalse,
        JumpPastElseBranches,
        JumpToLoopStart,
        CallFunction,
        PushNewSourceFunction,
        PushNewSourceList,
        PushSubscriptValue,
        PushSubscriptSliceValue,
        AssignSubscript,
        PushAttributeValue,
        AssignAttribute,
        PushNewSourceDictionary,
        BinaryOr,
        BinaryIn,
        BinaryNotIn,
        PushNewIteratorWithValue,
        JumpOrForIteratorNext,
        Pop,
        CoerceToStr
    }
}
