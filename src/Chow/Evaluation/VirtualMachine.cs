using Chow.Interpreter.Jit;
using Chow.Interpreter.Values;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Chow.Interpreter.Evaluation
{
    sealed class VirtualMachine
    {
        readonly Chunk _chunk;

        // TEMPORARY: name-keyed dict precedes the planned scope/lifetime class. The Operation.Operand
        // resolves to a name via Chunk.ReadVariableName; the value is then read/written here by name.
        // REVIEW VARIABLE ASSIGNMENT COMPILATION FOR EXTRA DETAILS.
        Dictionary<string, TaggedUnion> _variables = new Dictionary<string, TaggedUnion>();

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

            while (IsRemainingOperation())
            {
                switch (CurrentOperation.Code)
                {
                    case OperationCode.PushConstant:
                        valStack.Push(_chunk.ReadConstant(CurrentOperation.Operand));
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


                    case OperationCode.AssignToVariable:
                        // Operand -> name via Chunk; dict indexer handles both insert and overwrite.
                        string assignName = _chunk.ReadVariableName(CurrentOperation.Operand);
                        _variables[assignName] = valStack.Pop();
                        break;

                    case OperationCode.LoadVariable:
                        // Operand -> name via Chunk. Semantic analysis is responsible for ensuring the
                        // name exists before this op runs; KeyNotFoundException here is a contract violation.
                        string loadName = _chunk.ReadVariableName(CurrentOperation.Operand);
                        valStack.Push(_variables[loadName]);
                        break;

                    default:
                        throw new NotImplementedException($"Execution of {CurrentOperation.Code} is not implemented.");
                }

                MoveToNextOperation();
            }

            return valStack.Count == 0 ? TaggedUnion.None : valStack.Pop();
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

        // TODO: Remove when no longer needed. This is for debugging developement
        public List<(string name, TaggedUnion value)> GetVariableDebugInfo()
        {
            var debugInfo = new List<(string name, TaggedUnion value)>();

            foreach (KeyValuePair<string, TaggedUnion> kvp in _variables)
            {
                debugInfo.Add((kvp.Key, kvp.Value));
            }

            return debugInfo;
        }
    }
}
