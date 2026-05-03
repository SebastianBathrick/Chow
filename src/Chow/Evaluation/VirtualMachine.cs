using Chow.Jit;
using Chow.Values;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Chow.Evaluation
{
    sealed class VirtualMachine
    {
        readonly Chunk _chunk;
        int _opsListIndex;

        private Operation CurrentOperation => _chunk[_opsListIndex];

        public VirtualMachine(Chunk chunk)
        {
            _chunk = chunk ?? throw new ArgumentNullException(nameof(chunk));
            _opsListIndex = 0;
        }

        public TaggedUnion ExecuteChunk()
        {
            Stack<TaggedUnion> valStack = new Stack<TaggedUnion>();
            Dictionary<string, TaggedUnion> varMap = new Dictionary<string, TaggedUnion>();

            while (IsRemainingOperation())
            {
                switch (CurrentOperation.Code)
                {
                    case OperationCode.PushConstant:
                        // The operand is the index of the constant in the chunk's constant pool.
                        valStack.Push(_chunk.GetConstant(CurrentOperation.Operand));
                        break;

                    case OperationCode.Add:
                        ExecuteBinaryOperation(valStack, (l, r) => l + r);
                        break;

                    case OperationCode.Subtract:
                        ExecuteBinaryOperation(valStack, (l, r) => l - r);
                        break;

                    case OperationCode.Multiply:
                        ExecuteBinaryOperation(valStack, (l, r) => l * r);
                        break;

                    case OperationCode.Divide:
                        ExecuteBinaryOperation(valStack, (l, r) => l / r);
                        break;

                    case OperationCode.Modulus:
                        ExecuteBinaryOperation(valStack, (l, r) => l % r);
                        break;

                    case OperationCode.Exponentiate:
                        ExecuteBinaryOperation(valStack, (l, r) => TaggedUnion.Power(l, r));
                        break;

                    case OperationCode.FloorDivide:
                        ExecuteBinaryOperation(valStack, (l, r) => TaggedUnion.FloorDivide(l, r));
                        break;

                    case OperationCode.Negate:
                        ExecuteNegate(valStack);
                        break;

                    // Statements
                    case OperationCode.StoreVariable:
                        // The operand is the index of the variable name in the chunk's constant pool.
                        string varName = _chunk.GetConstant(CurrentOperation.Operand).StringValue;
                        varMap[varName] = valStack.Pop();
                        break;

                    default:
                        throw new NotImplementedException($"Execution of {CurrentOperation.Code} is not implemented.");
                }

                MoveToNextOperation();
            }

            return valStack.Count == 0 ? TaggedUnion.None : valStack.Pop();
        }

        void ExecuteCurrentOperation(Stack<TaggedUnion> stack)
        {

        }

        static void ExecuteBinaryOperation(Stack<TaggedUnion> stack, Func<TaggedUnion, TaggedUnion, TaggedUnion> operation)
        {
            // Floats coerce integers into floats inside TaggedUnion's operator overloads
            TaggedUnion right = stack.Pop();
            TaggedUnion left = stack.Pop();
            stack.Push(operation(left, right));
        }

        static void ExecuteNegate(Stack<TaggedUnion> stack)
        {
            TaggedUnion operand = stack.Pop();

            if (operand.IsFloat)
            {
                stack.Push(new TaggedUnion(-operand.FloatValue));
            }
            else
            {
                stack.Push(new TaggedUnion(-operand.IntegerValue));
            }
        }

        void MoveToNextOperation()
        {
            _opsListIndex++;
        }

        public bool IsRemainingOperation()
        {
            return _opsListIndex != _chunk.Count;
        }
    }
}
