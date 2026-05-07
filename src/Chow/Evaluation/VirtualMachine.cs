using Chow.Interpreter.Compilation;
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

        Stack<TaggedUnion> _valStack = new Stack<TaggedUnion>();
        int _opsListIndex;

        private Instruction CurrentOperation => _chunk[_opsListIndex];

        public VirtualMachine(Chunk chunk)
        {
            _chunk = chunk ?? throw new ArgumentNullException(nameof(chunk));
            _opsListIndex = 0;
        }

        public TaggedUnion ExecuteChunk()
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
                        // Temporarily return the value because only top-level code exists currently
                        if (_valStack.Count == 0)
                        {
                            return TaggedUnion.None;
                        }

                        return _valStack.Pop();

                    default:
                        throw new NotImplementedException($"Execution of {CurrentOperation.Code} is not implemented.");
                }

                MoveToNextOperation();
            }

            return _valStack.Count == 0 ? TaggedUnion.None : _valStack.Pop();
        }

        private void PushVariableValue()
        {
            // Operand -> name via Chunk. Semantic analysis is responsible for ensuring the
            // name exists before this op runs; KeyNotFoundException here is a contract violation.
            string loadName = _chunk.ReadVariableName(CurrentOperation.Operand);
            _valStack.Push(_variables[loadName]);
        }

        private void AssignOrDeclareVariable()
        {
            // Operand -> name via Chunk; dict indexer handles both insert and overwrite.
            string assignName = _chunk.ReadVariableName(CurrentOperation.Operand);
            _variables[assignName] = _valStack.Pop();
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
