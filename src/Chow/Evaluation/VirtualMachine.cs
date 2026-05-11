using Chow.Interpreter.Compilation;
using Chow.Interpreter.Exceptions;
using Chow.Interpreter.Hooks;
using Chow.Interpreter.Values.Internal;
using System;
using System.Collections.Generic;


namespace Chow.Interpreter.Evaluation
{
    sealed class VirtualMachine
    {
        readonly ModuleScope _moduleScope;
        readonly CallStack _callStack;

        Stack<TaggedUnion> _valStack;
        IExecutionHook _exprHook;

        Instruction CurrentOperation => _callStack.CurrentInstr;

        public TaggedUnion ValStackTop => _valStack.Count > 0 ? _valStack.Peek() : TaggedUnion.None;

        public VirtualMachine(Chunk chunk, ModuleScope moduleScope, IExecutionHook exprHook)
        {
            _moduleScope = moduleScope == null ? new ModuleScope() : moduleScope;
            _callStack = new CallStack(chunk, _moduleScope);
            _valStack = new Stack<TaggedUnion>();
            _exprHook = exprHook;
        }

        public ModuleScope ExecuteChunk()
        {
            while (_callStack.IsInstrToRun)
            {
                switch (CurrentOperation.Code)
                {
                    case OperationCode.PushConstant:
                        _valStack.Push(_callStack.CurrentChunk.ReadConstant(CurrentOperation.Operand));
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
                            _callStack.JumpToInstr(CurrentOperation.Operand);
                            continue;
                        }
                        _valStack.Pop();
                        break;

                    case OperationCode.JumpIfTrueOrPop:
                        if (_valStack.Peek().IsTruthy)
                        {
                            // Leave the truthy value on the stack as the result of the short-circuited `or`
                            _callStack.JumpToInstr(CurrentOperation.Operand);
                            continue;
                        }
                        _valStack.Pop();
                        break;

                    case OperationCode.JumpIfFalse:
                        // Always pops; jumps past the branch body when the condition is false
                        if (!_valStack.Pop().IsTruthy)
                        {
                            _callStack.JumpToInstr(CurrentOperation.Operand);
                            continue;
                        }
                        break;

                    case OperationCode.JumpPastBranches:
                        // Unconditional jump emitted at the end of a taken if/elif body to skip remaining branches
                        _callStack.JumpToInstr(CurrentOperation.Operand);
                        continue;

                    case OperationCode.Loop:
                        // Unconditional backward jump emitted at the bottom of a loop body (and for `continue`)
                        _callStack.JumpToInstr(CurrentOperation.Operand);
                        continue;

                    case OperationCode.IncScopeDepth:
                        _callStack.EnterNestedScope();
                        break;

                    case OperationCode.DecScopeDepth:
                        _callStack.ExitNestedScope();
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
                        _exprHook.Invoke(ApiValueConverter.ToApiClassObj(exprResult));
                        break;

                    case OperationCode.MakeClosure:
                        ExecuteMakeClosure();
                        break;

                    case OperationCode.Call:
                        if (ExecuteCall(CurrentOperation.Operand))
                        {
                            // A Closure was entered; caller's IP was already advanced and a new frame is active.
                            continue;
                        }
                        break;

                    case OperationCode.ReturnValue:
                        ExecuteReturnValue();
                        // Caller's IP was advanced before the call; resume the caller without auto-advancing the freshly-restored frame.
                        continue;

                    case OperationCode.BuildList:
                        ExecuteBuildList(CurrentOperation.Operand);
                        break;

                    case OperationCode.Subscript:
                        ExecuteSubscript();
                        break;

                    case OperationCode.SubscriptSlice:
                        ExecuteSubscriptSlice();
                        break;

                    case OperationCode.SubscriptSet:
                        ExecuteSubscriptSet();
                        break;

                    case OperationCode.GetAttr:
                        ExecuteGetAttr();
                        break;

                    case OperationCode.SetAttr:
                        ExecuteSetAttr();
                        break;

                    default:
                        throw new NotImplementedException($"Execution of {CurrentOperation.Code} is not implemented.");
                }

                _callStack.MoveToNextInstr();
            }

            return _moduleScope;
        }

        void ExecuteMakeClosure()
        {
            TaggedUnion templateUnion = _valStack.Pop();
            ClosureTemplate template = (ClosureTemplate)templateUnion.ObjectValue;
            Scope captured = _callStack.CurrentScope;
            Closure closure = new Closure(template.Chunk, captured, template.Name, template.ParamCount);
            _valStack.Push(new TaggedUnion((object)closure));
        }

        void ExecuteReturnValue()
        {
            TaggedUnion result = _valStack.Pop();
            _callStack.ExitFunctionCall();
            _valStack.Push(result);
        }

        private void PushVariableValue()
        {
            // Operand -> name via Chunk. Semantic analysis is responsible for ensuring the
            // name exists before this op runs; KeyNotFoundException here is a contract violation.
            string varName = _callStack.CurrentChunk.ReadVariableName(CurrentOperation.Operand);

            if (_callStack.IsVariableDefined(varName))
            {
                TaggedUnion varValue = _callStack.GetVariableValue(varName);
                _valStack.Push(varValue);
                return;
            }

            int errorLineNum = GetCurrentLineNumber();
            throw new ChowNameErrorException(varName, errorLineNum);
        }

        private void AssignOrDeclareVariable()
        {
            // Operand -> name via Chunk; CallStack routes the assign to the current frame's scope.
            string name = _callStack.CurrentChunk.ReadVariableName(CurrentOperation.Operand);
            TaggedUnion assignVal = _valStack.Pop();
            _callStack.AssignVariableValue(name, assignVal);
        }

        // Returns true when a Chow Closure was entered (frame pushed, caller IP already advanced).
        // Returns false for the synchronous interop path, where the result is already on the value stack.
        bool ExecuteCall(int argCount)
        {
            TaggedUnion[] args = new TaggedUnion[argCount];
            for (int i = argCount - 1; i >= 0; i--)
            {
                args[i] = _valStack.Pop();
            }
            TaggedUnion calleeUnion = _valStack.Pop();

            if (calleeUnion.Tag == Tag.Object && calleeUnion.ObjectValue is Closure closure)
            {
                if (argCount != closure.ParamCount)
                {
                    throw new ChowTypeErrorException(
                        $"{closure.Name}() takes {closure.ParamCount} positional arguments but {argCount} were given");
                }

                // Re-push args; function body's first ops are param-bind AssignOrDeclareVariable's, popping right-to-left.
                for (int i = 0; i < argCount; i++)
                {
                    _valStack.Push(args[i]);
                }

                // Advance caller's IP BEFORE pushing the frame so ReturnValue lands at the next caller instruction.
                _callStack.MoveToNextInstr();
                _callStack.EnterFunctionCall(closure);
                return true;
            }

            // Interop dispatch with already-popped values.
            TaggedUnion result;
            if (argCount == 0)
            {
                result = calleeUnion.MakeInteropCall(null, null);
            }
            else if (argCount == 1)
            {
                result = calleeUnion.MakeInteropCall(args[0], null);
            }
            else
            {
                result = calleeUnion.MakeInteropCall(null, args);
            }

            _valStack.Push(result);
            return false;
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

        void ExecuteNot()
        {
            TaggedUnion operand = _valStack.Pop();
            _valStack.Push(new TaggedUnion(!operand.IsTruthy));
        }

        int GetCurrentLineNumber()
        {
            return _callStack.CurrentLineNum;
        }

        void ExecuteBuildList(int elementCount)
        {
            // Pop N values; reverse so source order is preserved.
            TaggedUnion[] reversed = new TaggedUnion[elementCount];
            for (int i = elementCount - 1; i >= 0; i--)
            {
                reversed[i] = _valStack.Pop();
            }

            InternalList list = new InternalList();
            for (int i = 0; i < elementCount; i++)
            {
                list.Add(reversed[i]);
            }
            _valStack.Push(new TaggedUnion(list));
        }

        void ExecuteSubscript()
        {
            TaggedUnion index = _valStack.Pop();
            TaggedUnion target = _valStack.Pop();

            // FUTURE: strings/dicts add tag branches here.
            if (target.Tag == Tag.List)
            {
                if (index.Tag != Tag.Int)
                {
                    throw new ChowTypeErrorException($"list indices must be integers, not {index.Tag}");
                }
                _valStack.Push(target.ListValue[index.IntegerValue]);
                return;
            }

            throw new ChowTypeErrorException($"'{target.Tag}' object is not subscriptable");
        }

        void ExecuteSubscriptSlice()
        {
            TaggedUnion step = _valStack.Pop();
            TaggedUnion stop = _valStack.Pop();
            TaggedUnion start = _valStack.Pop();
            TaggedUnion target = _valStack.Pop();

            // FUTURE: strings add a parallel slice branch.
            if (target.Tag == Tag.List)
            {
                _valStack.Push(target.ListValue.GetSlice(start, stop, step));
                return;
            }

            throw new ChowTypeErrorException($"'{target.Tag}' object is not subscriptable");
        }

        void ExecuteSubscriptSet()
        {
            TaggedUnion value = _valStack.Pop();
            TaggedUnion index = _valStack.Pop();
            TaggedUnion target = _valStack.Pop();

            // FUTURE: dicts add a key-set branch.
            if (target.Tag == Tag.List)
            {
                if (index.Tag != Tag.Int)
                {
                    throw new ChowTypeErrorException($"list indices must be integers, not {index.Tag}");
                }
                target.ListValue[index.IntegerValue] = value;
                return;
            }

            throw new ChowTypeErrorException($"'{target.Tag}' object does not support item assignment");
        }

        void ExecuteGetAttr()
        {
            string attrName = _callStack.CurrentChunk.ReadVariableName(CurrentOperation.Operand);
            TaggedUnion target = _valStack.Pop();

            // FUTURE: class instances add a branch that consults the instance attribute table, then the class method table.
            if (target.Tag == Tag.List)
            {
                InternalList list = target.ListValue;
                if (!list.HasMethod(attrName))
                {
                    throw new ChowAttributeErrorException("list", attrName, GetCurrentLineNumber());
                }
                _valStack.Push(list[attrName]);
                return;
            }

            throw new ChowAttributeErrorException(target.Tag.ToString().ToLowerInvariant(), attrName, GetCurrentLineNumber());
        }

        void ExecuteSetAttr()
        {
            string attrName = _callStack.CurrentChunk.ReadVariableName(CurrentOperation.Operand);
            _valStack.Pop(); // value
            TaggedUnion target = _valStack.Pop();

            // FUTURE: class instances assign here.
            string typeName = target.Tag == Tag.List ? "list" : target.Tag.ToString().ToLowerInvariant();
            throw new ChowAttributeErrorException(typeName, attrName, GetCurrentLineNumber());
        }
    }
}
