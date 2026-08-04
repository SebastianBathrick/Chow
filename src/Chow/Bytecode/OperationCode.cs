using Chow.Interpreter.VM;

namespace Chow.Bytecode
{
    /// <summary>
    /// Represents an <see cref="Instruction"/>'s operation code, which is mapped to the logic inside the
    /// <see cref="Processor"/> class.
    /// </summary>
    enum OperationCode : byte
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
        AssignLocal,
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
        PushNewSourceClass,
        PushNewSourceList,
        PushSubscriptValue,
        PushSubscriptSliceValue,
        AssignSubscript,
        PushAttributeValue,
        AssignAttribute,
        PushNewSourceDictionary,
        BinaryUnion,
        BinaryIn,
        BinaryNotIn,
        PushNewIteratorWithValue,
        JumpOrForIteratorNext,
        Pop,
        CoerceToStr
    }
}
