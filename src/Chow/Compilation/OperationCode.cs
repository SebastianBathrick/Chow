namespace Chow.Interpreter.Jit
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
        StoreVariable,
        LoadVariable,
    }
}