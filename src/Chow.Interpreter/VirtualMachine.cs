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
        #region Fields

        readonly Scope _globalScope;
        readonly CallStack _callStack;
        readonly Stack<TaggedUnion> _valStack;

        #endregion

        #region Properties

        Instruction CurrentOperation => _callStack.CurrentInstr;

        public TaggedUnion ValStackTop => _valStack.Count > 0 ? _valStack.Peek() : TaggedUnion.None;

        #endregion

        #region Constructors

        public VirtualMachine(Chunk chunk, Scope globalScope)
            : this(globalScope, chunk)
        {
        }

        // Chunk is null when the client is exclusively calling a closure
        public VirtualMachine(Scope globalScope = null, Chunk chunk = null)
        {
            _globalScope = globalScope ?? new Scope();
            _callStack = new CallStack(chunk ?? new Chunk(), _globalScope);
            _valStack = new Stack<TaggedUnion>();
        }

        #endregion

        #region Public API
        
        public Scope EvaluateChunk()
        {
            while (_callStack.IsInstrToRun)
            {
                switch (CurrentOperation.Code)
                {
                    case OperationCode.PushConstant:
                    {
                        _valStack.Push(_callStack.CurrentChunk.ReadConstant(CurrentOperation.Operand));
                        break;
                    }

                    case OperationCode.CallFunction:
                    {
                        CallFunction(CurrentOperation.Operand, out var isClosureEntered);

                        if (isClosureEntered)
                        {
                            // A Closure was entered; caller's IP was already advanced and a new frame is active.
                            continue;
                        }

                        break;
                    }

                    #region Binary Operators

                    case OperationCode.Add:
                    {
                        EvaluateBinaryOperation((l, r) => l + r);
                        break;
                    }

                    case OperationCode.Subtract:
                    {
                        EvaluateBinaryOperation((l, r) => l - r);
                        break;
                    }

                    case OperationCode.Multiply:
                    {
                        EvaluateBinaryOperation((l, r) => l * r);
                        break;
                    }

                    case OperationCode.Divide:
                    {
                        EvaluateBinaryOperation((l, r) => l / r);
                        break;
                    }

                    case OperationCode.Modulus:
                    {
                        EvaluateBinaryOperation((l, r) => l % r);
                        break;
                    }

                    case OperationCode.Exponentiate:
                    {
                        EvaluateBinaryOperation(TaggedUnion.Power);
                        break;
                    }

                    case OperationCode.FloorDivide:
                    {
                        EvaluateBinaryOperation(TaggedUnion.FloorDivide);
                        break;
                    }

                    case OperationCode.Equal:
                    {
                        EvaluateBinaryOperation((l, r) => new TaggedUnion(l == r));
                        break;
                    }

                    case OperationCode.NotEqual:
                    {
                        EvaluateBinaryOperation((l, r) => new TaggedUnion(l != r));
                        break;
                    }

                    case OperationCode.Less:
                    {
                        EvaluateBinaryOperation((l, r) => new TaggedUnion(l < r));
                        break;
                    }

                    case OperationCode.Greater:
                    {
                        EvaluateBinaryOperation((l, r) => new TaggedUnion(l > r));
                        break;
                    }

                    case OperationCode.LessEqual:
                    {
                        EvaluateBinaryOperation((l, r) => new TaggedUnion(l <= r));
                        break;
                    }

                    case OperationCode.GreaterEqual:
                    {
                        EvaluateBinaryOperation((l, r) => new TaggedUnion(l >= r));
                        break;
                    }

                    case OperationCode.BinaryOr:
                    {
                        EvaluateBinaryOperation((l, r) => l | r);
                        break;
                    }

                    case OperationCode.In:
                    {
                        ExecuteIn(negate: false);
                        break;
                    }

                    case OperationCode.NotIn:
                    {
                        ExecuteIn(negate: true);
                        break;
                    }

                    #endregion

                    #region Negation

                    case OperationCode.Not:
                    {
                        EvaluateNot();
                        break;
                    }

                    case OperationCode.Negate:
                    {
                        EvaluateNegation();
                        break;
                    }

                    #endregion

                    #region Jumps

                    case OperationCode.JumpIfFalseOrPop:
                    {
                        if (!_valStack.Peek().IsTruthy)
                        {
                            // Leave the falsy value on the stack as the result of the short-circuited `and`
                            _callStack.JumpToInstr(CurrentOperation.Operand);
                            continue;
                        }

                        _valStack.Pop();
                        break;
                    }

                    case OperationCode.JumpIfTrueOrPop:
                    {
                        if (_valStack.Peek().IsTruthy)
                        {
                            // Leave the truthy value on the stack as the result of the short-circuited `or`
                            _callStack.JumpToInstr(CurrentOperation.Operand);
                            continue;
                        }

                        _valStack.Pop();
                        break;
                    }

                    case OperationCode.JumpIfFalse:
                    {
                        // Always pops; jumps past the branch body when the condition is false
                        if (!_valStack.Pop().IsTruthy)
                        {
                            _callStack.JumpToInstr(CurrentOperation.Operand);
                            continue;
                        }

                        break;
                    }

                    case OperationCode.JumpPastElseBranches:
                    {
                        // Unconditional jump emitted at the end of a taken if/elif body to skip remaining branches
                        _callStack.JumpToInstr(CurrentOperation.Operand);
                        continue;
                    }

                    case OperationCode.JumpToLoopStart:
                    {
                        // Unconditional backward jump emitted at the bottom of a loop body (and for `continue`)
                        _callStack.JumpToInstr(CurrentOperation.Operand);
                        continue;
                    }

                    #endregion

                    #region Push/Pop

                    case OperationCode.PopAndAssignToVariable:
                    {
                        PopAndAssignToVariable();
                        break;
                    }

                    case OperationCode.PushVariableValue:
                    {
                        PushVariableValue();
                        break;
                    }

                    case OperationCode.PushNewInternalList:
                    {
                        PushNewInternalList(CurrentOperation.Operand);
                        break;
                    }

                    case OperationCode.PushNewClosureFromTemplate:
                    {
                        PushNewClosureFromTemplate();
                        break;
                    }

                    case OperationCode.PushNewInternalDict:
                    {
                        PushNewInternalDict(CurrentOperation.Operand);
                        break;
                    }

                    case OperationCode.PushReturnValue:
                    {
                        PushReturnValue();
                        // Caller's IP was advanced before the call; resume the caller without auto-advancing the freshly-restored frame.
                        continue;
                    }

                    case OperationCode.PopExpressionStatementResult:
                    {
                        _valStack.Pop();
                        break;
                    }

                    #endregion

                    #region Subscripts

                    case OperationCode.Subscript:
                    {
                        ExecuteSubscript();
                        break;
                    }

                    case OperationCode.SubscriptSlice:
                    {
                        ExecuteSubscriptSlice();
                        break;
                    }

                    case OperationCode.SubscriptSet:
                    {
                        ExecuteSubscriptSet();
                        break;
                    }

                    #endregion

                    #region Attributes

                    case OperationCode.GetObjectAttribute:
                    {
                        GetObjectAttribute();
                        break;
                    }

                    case OperationCode.SetInteropObjectAttribute:
                    {
                        SetInteropObjectAttribute();
                        break;
                    }

                    #endregion

                    default:
                    {
                        throw new NotImplementedException($"Execution of {CurrentOperation.Code} is not implemented.");
                    }
                }

                _callStack.MoveToNextInstr();
            }

            return _globalScope;
        }

        /// <summary>
        /// Calls a function stored in a global variable with the name provided.
        /// </summary>
        /// <param name="callVarName">The name of a variable declared in the global scope</param>
        /// <param name="args">The arguments to pass to the function. If there are not any, this parameter can be null.</param>
        /// <returns>The result of the function call.</returns>
        /// <exception cref="UndefinedNameException">Thrown if the variable is not defined.</exception>
        /// <remarks>Assumes that there is a global scope already set up that was provided to the constructor.</remarks>
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

            CallFunction(argCount: args != null ? args.Count : 0, out var isClosure);

            if (isClosure)
            {
                EvaluateChunk();
            }

            return _valStack.Pop();
        }

        #endregion

        #region Push/Pop Methods

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
                throw new UndefinedNameException(varName, GetCurrentLineNumber());
            }

            var varValue = _callStack.GetVariableValue(varName);
            _valStack.Push(varValue);
        }

        void PopAndAssignToVariable()
        {
            // Operand -> name via Chunk; CallStack routes the assign to the current frame's scope.
            var name = _callStack.CurrentChunk.ReadVariableName(CurrentOperation.Operand);
            var assignVal = _valStack.Pop();

            _callStack.AssignVariableValue(name, assignVal);
        }

        void PushNewInternalList(int elementCount)
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

        void PushNewInternalDict(int pairCount)
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

        void PushNewClosureFromTemplate()
        {
            var poppedObject = _valStack.Pop().ObjectValue;

            if (!(poppedObject is ClosureTemplate template))
            {
                throw new InvalidOperationException("Expected a closure template");
            }

            var captured = _callStack.CurrentScope;
            var closure = new Closure(template.Chunk, captured, template.Name, template.ParamCount);

            _valStack.Push(new TaggedUnion(closure));
        }


        #endregion

        #region Function CallFunction Methods

        // Use an out parameter just so it's more explicit
        void CallFunction(int argCount, out bool isClosureEntered)
        {
            var args = new TaggedUnion[argCount];

            for (var i = argCount - 1; i >= 0; i--)
            {
                args[i] = _valStack.Pop();
            }

            var calleeUnion = _valStack.Pop();

            // If the tagged union is storing a closure inside (i.e. a function made up of bytecode)
            isClosureEntered = calleeUnion.Tag == Tag.Object && calleeUnion.ObjectValue is Closure;

            if (isClosureEntered)
            {
                // Switches to the closure's frame, so EvaluateChunk will next execute the first instruction of the closure's chunk.
                PushClosureStackFrame(argCount, (Closure)calleeUnion.ObjectValue, args);
            }
            else
            {
                // Will push its return value onto the stack.
                CallInteropFunction(argCount, calleeUnion, args);
            }
        }

        void CallInteropFunction(int argCount, TaggedUnion calleeUnion, TaggedUnion[] args)
        {
            // Interop dispatch with already-popped values.
            TaggedUnion result;

            // TODO: Refactor the MakeInteropCall method to avoid all these separate argument cases
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
        }

        void PushClosureStackFrame(int argCount, Closure closure, TaggedUnion[] args)
        {
            if (argCount != closure.ParamCount)
            {
                throw new TypeException($"{closure.Name}() takes {closure.ParamCount} positional arguments but {argCount} were given");
            }

            // Re-push args; function body's first ops are param-bind PopAndAssignToVariable's, popping right-to-left.
            for (var i = 0; i < argCount; i++)
            {
                _valStack.Push(args[i]);
            }

            // Advance caller's IP BEFORE pushing the frame so PushReturnValue lands at the next caller instruction.
            _callStack.MoveToNextInstr();
            _callStack.EnterFunctionCall(closure);
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
            TaggedUnion negatedUnion;

            if (operand.IsFloat)
            {
                negatedUnion = new TaggedUnion(-operand.FloatValue);
            }
            else
            {
                negatedUnion = new TaggedUnion(-operand.IntegerValue);
            }

            _valStack.Push(negatedUnion);
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
            var found = false;

            switch (container.Tag)
            {
                case Tag.Dict:
                {
                    found = container.DictValue.ContainsKey(needle);
                    break;
                }

                case Tag.List:
                {
                    var list = container.ListValue;

                    for (var i = 0; i < list.Count && !found; i++)
                    {
                        found = list[i] == needle;
                    }

                    break;
                }

                default:
                {
                    throw new TypeException($"argument of type '{container.Tag}' is not iterable");
                }
            }

            _valStack.Push(new TaggedUnion(negate ? !found : found));
        }

        #endregion

        #region Subscript Methods

        void ExecuteSubscript()
        {
            var index = _valStack.Pop();
            var target = _valStack.Pop();

            // TODO: Add a tag case here for strings.
            switch (target.Tag)
            {
                case Tag.Dict:
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
                case Tag.List:
                {
                    if (index.Tag != Tag.Int)
                    {
                        throw new TypeException($"list indices must be integers, not {index.Tag}");
                    }

                    _valStack.Push(target.ListValue[(int)index.IntegerValue]);
                    return;
                }
            }

            throw new TypeException($"'{ParseDataTypeName(target.Tag)}' object is not subscriptable");
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

            switch (target.Tag)
            {
                case Tag.Dict:
                {
                    target.DictValue[index] = value;
                    return;
                }

                case Tag.List:
                {
                    if (index.Tag != Tag.Int)
                    {
                        throw new TypeException($"list indices must be integers, not {index.Tag}");
                    }

                    target.ListValue[(int)index.IntegerValue] = value;
                    return;
                }
            }

            throw new TypeException($"'{ParseDataTypeName(target.Tag)}' object does not support item assignment");
        }

        #endregion

        #region Attributes Methods

        void GetObjectAttribute()
        {
            var attrName = _callStack.CurrentChunk.ReadVariableName(CurrentOperation.Operand);
            var target = _valStack.Pop();

            // TODO: class instances add a branch that consults the instance attribute table, then the class method table.
            switch (target.Tag)
            {
                case Tag.List:
                {
                    var list = target.ListValue;

                    if (!list.HasMethod(attrName))
                    {
                        throw new AttributeException(ParseDataTypeName(target.Tag), attrName, GetCurrentLineNumber());
                    }

                    _valStack.Push(list[attrName]);
                    return;
                }
                case Tag.Dict:
                {
                    var dict = target.DictValue;

                    if (!dict.HasMethod(attrName))
                    {
                        throw new AttributeException(ParseDataTypeName(target.Tag), attrName, GetCurrentLineNumber());
                    }

                    _valStack.Push(dict[attrName]);
                    return;
                }
                case Tag.Object when target.ObjectValue is InteropClassObject ico:
                {
                    if (!ico.HasAttribute(attrName))
                    {
                        throw new AttributeException(ParseDataTypeName(target.Tag), attrName, GetCurrentLineNumber());
                    }

                    _valStack.Push(ico.GetAttribute(attrName));
                    return;
                }
            }

            throw new AttributeException(ParseDataTypeName(target.Tag), attrName, GetCurrentLineNumber());
        }

        void SetInteropObjectAttribute()
        {
            // Lists and dicts are not included because their attributes are readonly
            var attrName = _callStack.CurrentChunk.ReadVariableName(CurrentOperation.Operand);
            var value = _valStack.Pop();
            var target = _valStack.Pop();

            if (target.Tag != Tag.Object || !(target.ObjectValue is InteropClassObject interopObject))
            {
                throw new AttributeException(ParseDataTypeName(target.Tag), attrName, GetCurrentLineNumber());
            }

            if (interopObject.CanSetAttribute(attrName))
            {
                interopObject.SetAttribute(attrName, value);
                return;
            }

            if (!interopObject.HasAttribute(attrName))
            {
                throw new AttributeException(interopObject.ClassName, attrName, GetCurrentLineNumber());
            }

                // Method names and read-only fields land here.
            throw new AttributeException(interopObject.ClassName, attrName, GetCurrentLineNumber(),
                    $"'{interopObject.ClassName}' object attribute '{attrName}' is read-only");
        }

        #endregion

        #region Helper Methods

        static string ParseDataTypeName(Tag dataTypeTag)
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
