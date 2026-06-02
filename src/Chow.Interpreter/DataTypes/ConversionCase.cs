namespace Chow.Interpreter.DataTypes
{
    // TODO: Add 'other' conversion cases, to specify whether it's the left or the right operand that gets converted
    enum ConversionCase
    {
        NoConversion,
        PromoteToInt,
        PromoteToFloat
    }
}
