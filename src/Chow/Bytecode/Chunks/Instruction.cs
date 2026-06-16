using Chow.VM;

namespace Chow.Bytecode
{
    /// <summary>Represents a single bytecode instruction, consisting of an operation code and an optional operand.</summary>
    readonly struct Instruction
    {
        const int NO_OPERAND = -1;

        /// <summary>Additional information that may be required for specific instructions, such as the index of a constant or
        /// variable name. Default value is <see cref="NO_OPERAND"/>, indicating no operand.</summary>
        public int Operand { get; }
        
        /// <summary>The code mapped to the logic of this instruction found inside the <see cref="InstructionProcessor"/>.</summary>
        public OperationCode Code { get; }

        /// <summary>Initializes a new instance of the Instruction class with the specified operation code and optional operand.</summary>
        /// <param name="type">The operation code that defines the type of instruction to create.</param>
        /// <param name="operand">The operand value associated with the instruction. Defaults to NO_OPERAND if not specified.</param>
        public Instruction(OperationCode type, int operand = NO_OPERAND)
        {
            Code = type;
            Operand = operand;
        }
    }
}
