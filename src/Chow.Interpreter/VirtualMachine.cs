using Chow.Interpreter.Bytecode;
using Chow.Interpreter.Exceptions;
using Chow.Interpreter.State.Scopes;
using Chow.Interpreter.State.Stack;
using Chow.Interpreter.State.Values;
using System.Collections.Generic;
using System;

namespace Chow.Interpreter
{
    sealed class VirtualMachine
    {
        readonly IScope _moduleScope;
        readonly CallStack _callStack;
        readonly Stack<TaggedUnion> _valStack;

        Instruction CurrentOperation => _callStack.CurrentInstr;

        public TaggedUnion ValStackTop => _valStack.Count > 0 ? _valStack.Peek() : TaggedUnion.None;

        public VirtualMachine(Chunk chunk, IScope moduleScope)
            : this(moduleScope, chunk)
        {
        }

        // Chunk is null when the client is exclusively calling a closure
        public VirtualMachine(IScope moduleScope = null, Chunk chunk = null)
        {
            _moduleScope = moduleScope ?? new ModuleScope();
            _callStack = new CallStack(chunk ?? new Chunk(), _moduleScope);
            _valStack = new Stack<TaggedUnion>();
        }

        public IScope EvaluateChunk()
        {
            while (_callStack.IsInstrToRun)
            {
                switch (CurrentOperation.Code)
                {
                    case OperationCode.PushConstant:
                        _valStack.Push(_callStack.CurrentChunk.ReadConstant(CurrentOperation.Operand));
                        break;

                    #region Binary Operators
                        
                    case OperationCode.Add:
                        EvaluateBinaryOperation((l, r) => l + r);
                        break;

                    case OperationCode.Subtract:
                        EvaluateBinaryOperation((l, r) => l - r);
                        break;

                    case OperationCode.Multiply:
                        EvaluateBinaryOperation((l, r) => l * r);
                        break;

                    case OperationCode.Divide:
                        EvaluateBinaryOperation((l, r) => l / r);
                        break;

                    case OperationCode.Modulus:
                        EvaluateBinaryOperation((l, r) => l % r);
                        break;

                    case OperationCode.Exponentiate:
                        EvaluateBinaryOperation(TaggedUnion.Power);
                        break;

                    case OperationCode.FloorDivide:
                        EvaluateBinaryOperation(TaggedUnion.FloorDivide);
                        break;

                    case OperationCode.Equal:
                        EvaluateBinaryOperation((l, r) => new TaggedUnion(l == r));
                        break;

                    case OperationCode.NotEqual:
                        EvaluateBinaryOperation((l, r) => new TaggedUnion(l != r));
                        break;

                    case OperationCode.Less:
                        EvaluateBinaryOperation((l, r) => new TaggedUnion(l < r));
                        break;

                    case OperationCode.Greater:
                        EvaluateBinaryOperation((l, r) => new TaggedUnion(l > r));
                        break;

                    case OperationCode.LessEqual:
                        EvaluateBinaryOperation((l, r) => new TaggedUnion(l <= r));
                        break;

                    case OperationCode.GreaterEqual:
                        EvaluateBinaryOperation((l, r) => new TaggedUnion(l >= r));
                        break;

                    case OperationCode.BinaryOr:
                        EvaluateBinaryOperation((l, r) => l | r);
                        break;

                    case OperationCode.In:
                        ExecuteIn(negate: false);
                        break;

                    case OperationCode.NotIn:
                        ExecuteIn(negate: true);
                        break;

                    #endregion

                    #region Negation

                    case OperationCode.Not:
                        EvaluateNot();
                        break;

                    case OperationCode.Negate:
                        EvaluateNegation();
                        break;

                    #endregion

                    #region Jumps

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

                    case OperationCode.JumpPastElseBranches:
                        // Unconditional jump emitted at the end of a taken if/elif body to skip remaining branches
                        _callStack.JumpToInstr(CurrentOperation.Operand);
                        continue;

                    case OperationCode.JumpToLoopStart:
                        // Unconditional backward jump emitted at the bottom of a loop body (and for `continue`)
                        _callStack.JumpToInstr(CurrentOperation.Operand);
                        continue;

                    #endregion

                    #region Scope (Needs Refactor)

                    // TODO: Refactor as the scope management system has changed to be more akin to Python's, without block scopes
                    case OperationCode.IncScopeDepth:
                        _callStack.EnterNestedScope();
                        break;

                    case OperationCode.DecScopeDepth:
                        _callStack.ExitNestedScope();
                        break;

                    #endregion

                    #region Variables

                    case OperationCode.VariableAssignOrDeclare:
                        PopToAssignToVariable();
                        break;

                    case OperationCode.VariablePushValue:
                        PushVariableValue();
                        break;

                    #endregion

                    case OperationCode.CreateClosureFromTemplate:
                        PushNewlyTemplatedClosure();
                        break;

                    case OperationCode.Call:
                        if (CallGlobalChowFunction(CurrentOperation.Operand))
                        {
                            // A Closure was entered; caller's IP was already advanced and a new frame is active.
                            continue;
                        }
                        break;

                    case OperationCode.ReturnValue:
                        PushReturnValue();
                        // Caller's IP was advanced before the call; resume the caller without auto-advancing the freshly-restored frame.
                        continue;

                    case OperationCode.PopExpressionStatementResult:
                        _valStack.Pop();
                        break;

                    #region Subscripts

                    case OperationCode.Subscript:
                        ExecuteSubscript();
                        break;

                    case OperationCode.SubscriptSlice:
                        ExecuteSubscriptSlice();
                        break;

                    case OperationCode.SubscriptSet:
                        ExecuteSubscriptSet();
                        break;

                    #endregion

                    #region Attributes

                    case OperationCode.GetVariableAttribute:
                        GetVariableAttribute();
                        break;

                    case OperationCode.SetVariableAttribute:
                        SetVariableAttribute();
                        break;

                    case OperationCode.CreateInternalDict:
                        PushNewlyBuiltDict(CurrentOperation.Operand);
                        break;

                    #endregion

                    #region Internal Data Structures

                    case OperationCode.CreateInternalList:
                        PushNewlyBuiltList(CurrentOperation.Operand);
                        break;

                    #endregion

                    default:
                        throw new NotImplementedException($"Execution of {CurrentOperation.Code} is not implemented.");
                }

                _callStack.MoveToNextInstr();
            }

            return _moduleScope;
        }

        #region Push/Pop Statement Methods

        void PushReturnValue()
        {
            // TODO: Revisit this after the scope system is refactored, this push-pop dance will likely be avoidable
            var result = _valStack.Pop();
            _callStack.ExitFunctionCall();

            _valStack.Push(result);
        }

        void PushVariableValue()
        {
            // Operand -> name via Chunk. Semantic analysis is responsible for ensuring the
            // name exists before this op runs; KeyNotFoundException here is a contract violation.
            var varName = _callStack.CurrentChunk.ReadVariableName(CurrentOperation.Operand);

            if (!_callStack.IsVariableDefined(varName))
            {
                var line = GetCurrentLineNumber();
                throw new UndefinedNameException(varName, line);
            }

            var varValue = _callStack.GetVariableValue(varName);
            _valStack.Push(varValue);
        }

        void PopToAssignToVariable()
        {
            // Operand -> name via Chunk; CallStack routes the assign to the current frame's scope.
            var name = _callStack.CurrentChunk.ReadVariableName(CurrentOperation.Operand);
            var assignVal = _valStack.Pop();

            _callStack.AssignVariableValue(name, assignVal);
        }

        #endregion

        #region Function Call Methods

        public TaggedUnion CallGlobalFunction(string callVarName, List<TaggedUnion> args)
        {
            if (!_callStack.IsVariableDefined(callVarName))
            {
                throw new UndefinedNameException(callVarName, -1);
            }

            _valStack.Push(_callStack.GetVariableValue(callVarName));

            if (args != null)
            {
                foreach (var arg in args)
                {
                    _valStack.Push(arg);
                }
            }

            var argCount = args == null ? 0 : args.Count;
            if (CallGlobalChowFunction(argCount))
            {
                EvaluateChunk();
            }

            return _valStack.Pop();
        }

        // Returns true when a Chow Closure was entered (frame pushed, caller IP already advanced).
        // Returns false for the synchronous interop path, where the result is already on the value stack.
        bool CallGlobalChowFunction(int argCount)
        {
            var args = new TaggedUnion[argCount];

            for (var i = argCount - 1; i >= 0; i--)
            {
                args[i] = _valStack.Pop();
            }
            var calleeUnion = _valStack.Pop();

            if (calleeUnion.Tag == Tag.Object && calleeUnion.ObjectValue is Closure closure)
            {
                return CallClosure(argCount, closure, args);
            }

            return CallGlobalInteropFunction(argCount, calleeUnion, args);
        }

        private bool CallGlobalInteropFunction(int argCount, TaggedUnion calleeUnion, TaggedUnion[] args)
        {
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

        private bool CallClosure(int argCount, Closure closure, TaggedUnion[] args)
        {
            if (argCount != closure.ParamCount)
            {
                throw new TypeException(
                    $"{closure.Name}() takes {closure.ParamCount} positional arguments but {argCount} were given");
            }

            // Re-push args; function body's first ops are param-bind VariableAssignOrDeclare's, popping right-to-left.
            for (var i = 0; i < argCount; i++)
            {
                _valStack.Push(args[i]);
            }

            // Advance caller's IP BEFORE pushing the frame so ReturnValue lands at the next caller instruction.
            _callStack.MoveToNextInstr();
            _callStack.EnterFunctionCall(closure);
            return true;
        }

        #endregion

        #region Expression Evaluation Methods

        void EvaluateBinaryOperation(Func<TaggedUnion, TaggedUnion, TaggedUnion> operation)
        {
            // Floats coerce integers into floats inside TaggedUnion's operator overloads
            var right = _valStack.Pop();
            var left = _valStack.Pop();
            _valStack.Push(operation(left, right));
        }

        void EvaluateNegation()
        {
            var operand = _valStack.Pop();

            _valStack.Push(operand.IsFloat 
                ? new TaggedUnion(-operand.FloatValue) 
                : new TaggedUnion(-operand.IntegerValue));
        }

        void EvaluateNot()
        {
            var operand = _valStack.Pop();
            _valStack.Push(new TaggedUnion(!operand.IsTruthy));
        }

        void ExecuteIn(bool negate)
        {
            var container = _valStack.Pop();
            var needle = _valStack.Pop();

            bool found;
            switch (container.Tag)
            {
                case Tag.Dict:
                    found = container.DictValue.ContainsKey(needle);
                    break;
                case Tag.List:
                    found = false;
                    var list = container.ListValue;
                    for (var i = 0; i < list.Count; i++)
                    {
                        if (list[i] == needle)
                        {
                            found = true;
                            break;
                        }
                    }
                    break;
                default:
                    throw new TypeException($"argument of type '{container.Tag}' is not iterable");
            }

            _valStack.Push(new TaggedUnion(negate ? !found : found));
        }

        #endregion

        #region Create Reference Type Value Methods

        void PushNewlyBuiltList(int elementCount)
        {
            // Pop N values; reverse so source order is preserved.
            var reversed = new TaggedUnion[elementCount];

            for (var i = elementCount - 1; i >= 0; i--)
            {
                reversed[i] = _valStack.Pop();
            }

            var list = new InternalList();

            for (var i = 0; i < elementCount; i++)
            {
                list.Add(reversed[i]);
            }

            _valStack.Push(new TaggedUnion(list));
        }

        void PushNewlyBuiltDict(int pairCount)
        {
            // Pop 2N values (value, key, value, key, ...); rebuild source order before insertion.
            var keys = new TaggedUnion[pairCount];
            var values = new TaggedUnion[pairCount];

            for (var i = pairCount - 1; i >= 0; i--)
            {
                values[i] = _valStack.Pop();
                keys[i] = _valStack.Pop();
            }

            var dict = new InternalDict();

            for (var i = 0; i < pairCount; i++)
            {
                dict.Add(keys[i], values[i]);
            }

            _valStack.Push(new TaggedUnion(dict));
        }

        void PushNewlyTemplatedClosure()
        {
            var templateUnion = _valStack.Pop();
            var template = (ClosureTemplate)templateUnion.ObjectValue;

            var captured = _callStack.CurrentScope;
            var closure = new Closure(template.Chunk, captured, template.Name, template.ParamCount);

            _valStack.Push(new TaggedUnion(closure));
        }

        #endregion

        #region Subscript Methods

        void ExecuteSubscript()
        {
            var index = _valStack.Pop();
            var target = _valStack.Pop();

            // FUTURE: strings add a tag branch here.
            if (target.Tag == Tag.Dict)
            {
                try
                {
                    _valStack.Push(target.DictValue[index]);
                }
                catch (DictKeyException ex)
                {
                    throw new DictKeyException(ex.KeyRepr, GetCurrentLineNumber());
                }
                return;
            }

            if (target.Tag != Tag.List)
            {
                throw new TypeException($"'{target.Tag}' object is not subscriptable");
            }

            if (index.Tag != Tag.Int)
            {
                throw new TypeException($"list indices must be integers, not {index.Tag}");
            }

            _valStack.Push(target.ListValue[(int)index.IntegerValue]);
        }

        void ExecuteSubscriptSlice()
        {
            var step = _valStack.Pop();
            var stop = _valStack.Pop();
            var start = _valStack.Pop();
            var target = _valStack.Pop();

            // FUTURE: strings add a parallel slice branch.
            if (target.Tag != Tag.List)
            {
                throw new TypeException($"'{target.Tag}' object is not subscriptable");
            }

            _valStack.Push(target.ListValue.GetSlice(start, stop, step));
        }

        void ExecuteSubscriptSet()
        {
            var value = _valStack.Pop();
            var index = _valStack.Pop();
            var target = _valStack.Pop();

            if (target.Tag == Tag.Dict)
            {
                target.DictValue[index] = value;
                return;
            }

            if (target.Tag != Tag.List)
            {
                throw new TypeException($"'{target.Tag}' object does not support item assignment");
            }

            if (index.Tag != Tag.Int)
            {
                throw new TypeException($"list indices must be integers, not {index.Tag}");
            }

            target.ListValue[(int)index.IntegerValue] = value;
        }

        #endregion

        #region Attributes Methods

        void GetVariableAttribute()
        {
            var attrName = _callStack.CurrentChunk.ReadVariableName(CurrentOperation.Operand);
            var target = _valStack.Pop();

            // FUTURE: class instances add a branch that consults the instance attribute table, then the class method table.
            if (target.Tag == Tag.List)
            {
                var list = target.ListValue;

                if (!list.HasMethod(attrName))
                {
                    throw new AttributeException(GetDataTypeName(target.Tag), attrName, GetCurrentLineNumber());
                }

                _valStack.Push(list[attrName]);
            }
            else if (target.Tag == Tag.Dict)
            {
                var dict = target.DictValue;

                if (!dict.HasMethod(attrName))
                {
                    throw new AttributeException(GetDataTypeName(target.Tag), attrName, GetCurrentLineNumber());
                }

                _valStack.Push(dict[attrName]);
            }
            else if (target.Tag == Tag.Object && target.ObjectValue is InteropClassObject ico)
            {
                if (!ico.HasAttribute(attrName))
                {
                    throw new AttributeException(GetDataTypeName(target.Tag), attrName, GetCurrentLineNumber());
                }
                _valStack.Push(ico.GetAttribute(attrName));
            }
            else
            {
                throw new AttributeException(GetDataTypeName(target.Tag), attrName, GetCurrentLineNumber());
            }
        }

        void SetVariableAttribute()
        {
            var attrName = _callStack.CurrentChunk.ReadVariableName(CurrentOperation.Operand);
            var value = _valStack.Pop();
            var target = _valStack.Pop();

            if (target.Tag == Tag.Object && target.ObjectValue is InteropClassObject ico)
            {
                if (!ico.CanSetAttribute(attrName))
                {
                    if (!ico.HasAttribute(attrName))
                    {
                        throw new AttributeException(ico.ClassName, attrName, GetCurrentLineNumber());
                    }

                    // Method names and read-only fields land here.
                    throw new AttributeException(
                        ico.ClassName, attrName, GetCurrentLineNumber(),
                        $"'{ico.ClassName}' object attribute '{attrName}' is read-only");
                }
                ico.SetAttribute(attrName, value);
            }
            else
            {
                throw new AttributeException(GetDataTypeName(target.Tag), attrName, GetCurrentLineNumber());
            }
        }

        #endregion

        #region Helper Methods

        static string GetDataTypeName(Tag dataTypeTag)
        {
            // TODO: Refactor so there's a single source of truth for datatype names used in error messages
            return dataTypeTag.ToString().ToLowerInvariant();
        }

        // TODO: Refactor to get rid of this method, as VirtualMachine no longer indexes the instruction stream directly (CallStack does)
        int GetCurrentLineNumber()
        {
            return _callStack.CurrentLineNum;
        }

        #endregion
    }
}
