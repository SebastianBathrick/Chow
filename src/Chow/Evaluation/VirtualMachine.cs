using Chow.Interpreter.Compilation;
using Chow.Interpreter.Exceptions;
using Chow.Interpreter.Hooks;
using Chow.Interpreter.Values;
using System;
using System.Collections.Generic;

namespace Chow.Interpreter.Evaluation
{
    sealed class VirtualMachine
    {
        readonly Chunk _chunk;
        readonly ChowEnviro _enviro;

        Stack<TaggedUnion> _valStack;
        IExecutionHook _exprHook;
        int _instructIdx;

        private Instruction CurrentOperation => _chunk[_instructIdx];

        public TaggedUnion ValStackTop => _valStack.Count > 0 ? _valStack.Peek() : TaggedUnion.None;

        public VirtualMachine(Chunk chunk, ChowEnviro enviro, IExecutionHook exprHook)
        {
            _chunk = chunk;
            _enviro = enviro == null ? new ChowEnviro() : enviro;
            _valStack = new Stack<TaggedUnion>();
            _instructIdx = 0;
            _exprHook = exprHook;
        }

        public ChowEnviro ExecuteChunk()
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

                    case OperationCode.Equal:
                        ExecuteBinaryOperation((l, r) => new TaggedUnion(l == r));
                        break;

                    case OperationCode.NotEqual:
                        ExecuteBinaryOperation((l, r) => new TaggedUnion(l != r));
                        break;

                    case OperationCode.Less:
                        ExecuteBinaryOperation((l, r) => new TaggedUnion(l < r));
                        break;

                    case OperationCode.Greater:
                        ExecuteBinaryOperation((l, r) => new TaggedUnion(l > r));
                        break;

                    case OperationCode.LessEqual:
                        ExecuteBinaryOperation((l, r) => new TaggedUnion(l <= r));
                        break;

                    case OperationCode.GreaterEqual:
                        ExecuteBinaryOperation((l, r) => new TaggedUnion(l >= r));
                        break;

                    case OperationCode.Not:
                        ExecuteNot();
                        break;

                    case OperationCode.JumpIfFalseOrPop:
                        if (!_valStack.Peek().IsTruthy)
                        {
                            // Leave the falsy value on the stack as the result of the short-circuited `and`
                            _instructIdx = CurrentOperation.Operand;
                            continue;
                        }
                        _valStack.Pop();
                        break;

                    case OperationCode.JumpIfTrueOrPop:
                        if (_valStack.Peek().IsTruthy)
                        {
                            // Leave the truthy value on the stack as the result of the short-circuited `or`
                            _instructIdx = CurrentOperation.Operand;
                            continue;
                        }
                        _valStack.Pop();
                        break;

                    // Statements

                    case OperationCode.AssignOrDeclareVariable:
                        AssignOrDeclareVariable();
                        break;

                    case OperationCode.PushVariableValue:
                        PushVariableValue();
                        break;

                    case OperationCode.PopExprStmntResult:
                        // Pop the result of an expression statement and ignore it, but trigger the expression statement hook for debugging
                        if (_exprHook == null)
                        {
                            _valStack.Pop();
                            break;
                        }

                        TaggedUnion exprResult = _valStack.Pop();
                        _exprHook.Invoke(ApiValueConverter.ToChowValue(exprResult));
                        break;

                    case OperationCode.ReturnValue:
                        throw new NotImplementedException();

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
            string varName = _chunk.ReadVariableName(CurrentOperation.Operand);

            if (_enviro.IsVariableDefined(varName))
            {
                TaggedUnion varValue = _enviro.GetVariableValue(varName);
                _valStack.Push(varValue);
                return;
            }

            int errorLineNum = GetCurrentLineNumber();
            throw new NameErrorException(varName, errorLineNum);
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
            }
            else
            {
                _valStack.Push(new TaggedUnion(-operand.IntegerValue));
            }
        }

        void ExecuteNot()
        {
            TaggedUnion operand = _valStack.Pop();
            _valStack.Push(new TaggedUnion(!operand.IsTruthy));
        }

        int GetCurrentLineNumber()
        {
            return _chunk.GetInstructionLineNumber(_instructIdx);
        }

        void MoveToNextOperation()
        {
            _instructIdx++;
        }

        public bool IsRemainingOperation()
        {
            return _instructIdx != _chunk.Count;
        }
    }
}
