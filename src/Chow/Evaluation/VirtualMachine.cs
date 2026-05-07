using Chow.Interpreter.Compilation;
using Chow.Interpreter.Values;
using System;
using System.Collections.Generic;

namespace Chow.Interpreter.Evaluation
{
    sealed class VirtualMachine
    {
        readonly Chunk _chunk;
        readonly ChowEnvironment _enviro = new ChowEnvironment();

        Stack<TaggedUnion> _valStack = new Stack<TaggedUnion>();
        int _opsListIndex;

        private Instruction CurrentOperation => _chunk[_opsListIndex];

        public VirtualMachine(Chunk chunk, ChowEnvironment enviro)
        {
            _chunk = chunk;
            _enviro = enviro == null ? new ChowEnvironment() : enviro;
            _opsListIndex = 0;
        }

        public ChowEnvironment ExecuteChunk()
        {
            while (IsRemainingOperation())
            {
                switch (CurrentOperation.Code)
                {
                    case OperationCode.PushConstant:
                        _valStack.Push(_chunk.ReadConstant(CurrentOperation.Operand));
                        break;

                    case OperationCode.Add:
                        ExecuteBinaryOperation((l, r) => l + r);
                        break;

                    case OperationCode.Subtract:
                        ExecuteBinaryOperation((l, r) => l - r);
                        break;

                    case OperationCode.Multiply:
                        ExecuteBinaryOperation((l, r) => l * r);
                        break;

                    case OperationCode.Divide:
                        ExecuteBinaryOperation((l, r) => l / r);
                        break;

                    case OperationCode.Modulus:
                        ExecuteBinaryOperation((l, r) => l % r);
                        break;

                    case OperationCode.Exponentiate:
                        ExecuteBinaryOperation((l, r) => TaggedUnion.Power(l, r));
                        break;

                    case OperationCode.FloorDivide:
                        ExecuteBinaryOperation((l, r) => TaggedUnion.FloorDivide(l, r));
                        break;

                    case OperationCode.Negate:
                        ExecuteNegate();
                        break;

                    // Statements

                    case OperationCode.AssignOrDeclareVariable:
                        AssignOrDeclareVariable();
                        break;

                    case OperationCode.PushVariableValue:
                        PushVariableValue();
                        break;

                    case OperationCode.ReturnValue:
                        // Temporarily allow return statements on top-level and print the return value to the console for debugging
                        if (_valStack.Count == 0)
                        {
                            Console.WriteLine(TaggedUnion.None);
                            return _enviro;
                        }

                        Console.WriteLine(_valStack.Pop());
                        break;

                    default:
                        throw new NotImplementedException($"Execution of {CurrentOperation.Code} is not implemented.");
                }

                MoveToNextOperation();
            }

            return _enviro;
        }

        private void PushVariableValue()
        {
            // Operand -> name via Chunk. Semantic analysis is responsible for ensuring the
            // name exists before this op runs; KeyNotFoundException here is a contract violation.
            string loadName = _chunk.ReadVariableName(CurrentOperation.Operand);
            _valStack.Push(_enviro.GetVariableValue(loadName));
        }

        private void AssignOrDeclareVariable()
        {
            // Operand -> name via Chunk; dict indexer handles both insert and overwrite.
            string assignName = _chunk.ReadVariableName(CurrentOperation.Operand);
            TaggedUnion assignVal = _valStack.Pop();
            _enviro.AssignVariableValue(assignName, assignVal);
        }

        void ExecuteBinaryOperation(Func<TaggedUnion, TaggedUnion, TaggedUnion> operation)
        {
            // Floats coerce integers into floats inside TaggedUnion's operator overloads
            TaggedUnion right = _valStack.Pop();
            TaggedUnion left = _valStack.Pop();
            _valStack.Push(operation(left, right));
        }

        void ExecuteNegate()
        {
            TaggedUnion operand = _valStack.Pop();

            if (operand.IsFloat)
            {
                _valStack.Push(new TaggedUnion(-operand.FloatValue));
                return;
            }

            _valStack.Push(new TaggedUnion(-operand.IntegerValue));
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
