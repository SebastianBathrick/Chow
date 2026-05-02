using Chow.Bytecode;
using Chow.Values;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chow
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
            Stack<TaggedUnion> runtimeValStack = new Stack<TaggedUnion>();

            while (IsRemainingOperation())
            {
                ExecuteCurrentOperation(runtimeValStack);
                MoveToNextOperation();
            }

            return runtimeValStack.Count == 0 ? TaggedUnion.None : runtimeValStack.Pop();
        }

        void ExecuteCurrentOperation(Stack<TaggedUnion> stack)
        {
            switch (CurrentOperation.Code)
            {
                case OperationCode.PushConstant:
                    // The operand is the index of the constant in the chunk's constant pool.
                    stack.Push(_chunk.GetConstant(CurrentOperation.Operand));
                    break;

                case OperationCode.Add:
                    ExecuteBinaryOperation(stack, (l, r) => l + r);
                    break;

                case OperationCode.Subtract:
                    ExecuteBinaryOperation(stack, (l, r) => l - r);
                    break;

                case OperationCode.Multiply:
                    ExecuteBinaryOperation(stack, (l, r) => l * r);
                    break;

                case OperationCode.Divide:
                    ExecuteBinaryOperation(stack, (l, r) => l / r);
                    break;

                case OperationCode.Modulus:
                    ExecuteBinaryOperation(stack, (l, r) => l % r);
                    break;

                case OperationCode.Exponentiate:
                    ExecuteBinaryOperation(stack, (l, r) => TaggedUnion.Power(l, r));
                    break;

                case OperationCode.FloorDivide:
                    ExecuteBinaryOperation(stack, (l, r) => TaggedUnion.FloorDivide(l, r));
                    break;

                case OperationCode.Negate:
                    ExecuteNegate(stack);
                    break;

                default:
                    throw new NotImplementedException($"Execution of {CurrentOperation.Code} is not implemented.");
            }
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
