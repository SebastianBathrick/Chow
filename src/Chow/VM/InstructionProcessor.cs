using System;
using System.Collections.Generic;
using Chow.Bytecode;
using Chow.SourceData;
using Chow.Utility;
using Chow.VM.FunctionCalls;

namespace Chow.VM
{
    sealed class InstructionProcessor
    {
        const bool GoToNextInstruction = true;
        const bool StayAtInstruction = false;

        readonly CallStack _callStack;
        readonly Stack<SourceValue> _valStack;
        SourceValue _expressionStatementVal = SourceValue.None;

        // BytecodeChunk is null when the client is exclusively calling a closure
        public InstructionProcessor(Scope globalScope = null, BytecodeChunk bytecodeChunk = null)
        {
            _callStack = new CallStack(bytecodeChunk ?? new BytecodeChunk(), globalScope);
            _valStack = new Stack<SourceValue>();
        }

        public SourceValue Execute()
        {
            while (_callStack.IsInstructionToExecute)
            {
                if (ExecuteInstruction())
                {
                    _callStack.MoveToNextInstruction();
                }
            }

            return _expressionStatementVal;
        }

        bool ExecuteInstruction()
        {
            // Read the instruction once; the property chain (CallStack -> frame -> bytecodeChunk indexer)
            // is hot enough that re-deriving it per Operand access shows up in dispatch cost.
            var instr = _callStack.CurrentInstr;

            switch (instr.Code)
            {
                case OperationCode.Pop:
                    _valStack.Pop();
                    break;
                case OperationCode.PushConstantValue:
                    _valStack.Push(_callStack.CurrentBytecodeChunk.ReadConstant(instr.Operand));
                    break;

                // -- Binary Operations------------------------------------------------------------
                case OperationCode.BinaryAdd:
                    _valStack.Push(SourceValue.Add(_valStack.Pop(), _valStack.Pop()));
                    break;
                case OperationCode.BinarySubtract:
                    _valStack.Push(SourceValue.Subtract(_valStack.Pop(), _valStack.Pop()));
                    break;
                case OperationCode.BinaryMultiply:
                    _valStack.Push(SourceValue.Multiply(_valStack.Pop(), _valStack.Pop()));
                    break;
                case OperationCode.BinaryDivide:
                    _valStack.Push(SourceValue.Divide(_valStack.Pop(), _valStack.Pop()));
                    break;
                case OperationCode.BinaryModulus:
                    _valStack.Push(SourceValue.Mod(_valStack.Pop(), _valStack.Pop()));
                    break;
                case OperationCode.BinaryPow:
                    _valStack.Push(SourceValue.Pow(_valStack.Pop(), _valStack.Pop()));
                    break;
                case OperationCode.BinaryFloor:
                    _valStack.Push(SourceValue.Floor(_valStack.Pop(), _valStack.Pop()));
                    break;
                case OperationCode.BinaryEqual:
                    _valStack.Push(SourceValue.IsEqual(_valStack.Pop(), _valStack.Pop()));
                    break;
                case OperationCode.BinaryNotEqual:
                    _valStack.Push(SourceValue.IsNotEqual(_valStack.Pop(), _valStack.Pop()));
                    break;
                case OperationCode.BinaryLess:
                    _valStack.Push(SourceValue.IsLess(_valStack.Pop(), _valStack.Pop()));
                    break;
                case OperationCode.BinaryGreater:
                    _valStack.Push(SourceValue.IsGreater(_valStack.Pop(), _valStack.Pop()));
                    break;
                case OperationCode.BinaryLessEqual:
                    _valStack.Push(SourceValue.IsLessOrEqual(_valStack.Pop(), _valStack.Pop()));
                    break;
                case OperationCode.BinaryGreaterEqual:
                    _valStack.Push(SourceValue.IsGreaterOrEqual(_valStack.Pop(), _valStack.Pop()));
                    break;
                case OperationCode.BinaryUnion:
                    ExecuteBinaryUnion();
                    break;
                case OperationCode.BinaryIn:
                    // TODO: Add 'in' to the LogicEvaluator
                    EvaluateIn(negate: false);
                    break;
                case OperationCode.BinaryNotIn:
                    // TODO: Add 'not in' to the LogicEvaluator
                    EvaluateIn(negate: true);
                    break;

                // -- Unary Operations-------------------------------------------------------------
                case OperationCode.UnaryNot:
                    _valStack.Push(SourceValue.Not(_valStack.Pop()));
                    break;
                case OperationCode.UnaryNegate:
                    _valStack.Push(SourceValue.Negate(_valStack.Pop()));
                    break;

                // -- Control Structure Operations ------------------------------------------------
                case OperationCode.JumpIfFalseOrPop:
                    return ExecuteJumpIfFalseOrPop(instr.Operand);
                case OperationCode.JumpIfTrueOrPop:
                    return ExecuteJumpIfTrueOrPop(instr.Operand);
                case OperationCode.JumpIfFalse:
                    return ExecuteJumpIfFalse(instr.Operand);
                case OperationCode.JumpPastElseBranches:
                    return ExecuteJump(instr.Operand);
                case OperationCode.JumpToLoopStart:
                    return ExecuteJump(instr.Operand);

                // -- Variable Operations ---------------------------------------------------------
                case OperationCode.AssignLocal:
                    ExecuteAssignVariable(instr.Operand);
                    break;
                case OperationCode.PushVariableValue:
                    ExecutePushVariableValue(instr.Operand);
                    break;
                case OperationCode.AssignGlobal:
                    ExecuteAssignGlobal(instr.Operand);
                    break;
                case OperationCode.PushGlobalValue:
                    ExecutePushGlobalValue(instr.Operand);
                    break;
                case OperationCode.AssignNonLocal:
                    ExecuteAssignNonLocal(instr.Operand);
                    break;
                case OperationCode.PushNonLocalValue:
                    ExecutePushNonLocalValue(instr.Operand);
                    break;

                // -- Attribute Operations --------------------------------------------------------
                case OperationCode.AssignAttribute:
                    ExecuteAssignAttribute(instr.Operand);
                    break;
                case OperationCode.PushAttributeValue:
                    ExecutePushAttribute(instr.Operand);
                    break;

                // -- Collection Operations -------------------------------------------------------
                case OperationCode.PushNewSourceList:
                    ExecutePushNewSourceList(instr.Operand);
                    break;
                case OperationCode.PushNewSourceDictionary:
                    ExecutePushNewSourceDictionary(instr.Operand);
                    break;

                // -- Iterator Operations ---------------------------------------------------------
                case OperationCode.PushNewIteratorWithValue:
                    ExecutePushNewIterator();
                    break;
                case OperationCode.JumpOrForIteratorNext:
                    return ExecuteJumpOrIterateFor(instr.Operand);

                // -- Subscript Operations ---------------------------------------------------------
                case OperationCode.AssignSubscript:
                    ExecuteAssignSubscript();
                    break;
                case OperationCode.PushSubscriptValue:
                    ExecutePushSubscriptValue();
                    break;
                case OperationCode.PushSubscriptSliceValue:
                    ExecutePushSubscriptSliceValue();
                    break;

                // -- Function Call Operations ----------------------------------------------------
                case OperationCode.CallFunction:
                    // If false, the bytecodeChunk will have switched to the called closure's
                    return ExecuteCallFunction(instr.Operand);
                case OperationCode.PushReturnValue:
                    _callStack.ExitFunctionCall();
                    return StayAtInstruction;
                case OperationCode.PushNewSourceFunction:
                    ExecutePushNewSourceFunction();
                    break;

                // -- Expression Evaluation Operations --------------------------------------------
                case OperationCode.CoerceToStr:
                    _valStack.Push(new SourceValue(_valStack.Pop().ToString()));
                    break;
                case OperationCode.PopExpressionStatementResult:
                    _expressionStatementVal = _valStack.Pop();
                    break;

                default:
                    throw new NotImplementedException($"Execution of {instr.Code} is not implemented.");
            }

            return GoToNextInstruction;
        }

        /// <summary>Calls a function stored in a global variable with the name provided.</summary>
        /// <param name="callVarName">
        /// The name of a variable declared in the global scope. Caller
        /// is responsible for verifying the name is defined.
        /// </param>
        /// <param name="args">
        /// The arguments to pass to the function. If there aren't any, this
        /// parameter can be null.
        /// </param>
        /// <returns>The result of the function call.</returns>
        /// <remarks>
        /// Assumes that there is a global scope already set up that was provided to the
        /// constructor.
        /// </remarks>
        public SourceValue CallGlobalFunction(string callVarName, SourceValue[] args)
        {
            _valStack.Push(_callStack.GetVariableValue(callVarName));

            if (args != null)
            {
                foreach (var arg in args)
                {
                    _valStack.Push(arg);
                }
            }

            if (ExecuteCallFunction(args?.Length ?? 0) == StayAtInstruction)
            {
                Execute();
            }

            return _valStack.Pop();
        }

        #region Binary Operations

        void ExecuteBinaryUnion()
        {
            _valStack.Push(SourceValue.Unite(_valStack.Pop(), _valStack.Pop()));
        }

        void EvaluateIn(bool negate)
        {
            var container = _valStack.Pop();
            var needle = _valStack.Pop();
            bool found;

            if (container.DataType == DataType.Dict || container.DataType == DataType.List)
            {
                found = container.ToISourceObject().Contains(needle);
            }
            else
            {
                throw new DataTypeException($"argument of type '{container.DataType}' is not iterable");
            }

            _valStack.Push(new SourceValue(negate ? !found : found));
        }

        #endregion

        #region Control Structure Operations

        bool ExecuteJumpIfFalseOrPop(int jumpTarget)
        {
            var operand = _valStack.Peek();

            // TODO: Move this to SourceValue
            if (LogicEvaluator.ShouldShortCircuitAnd(ref operand))
            {
                // Leave the falsy value on the stack as the result of the short-circuited `and`
                _callStack.JumpToInstr(jumpTarget);
                return StayAtInstruction;
            }

            _valStack.Pop();
            return GoToNextInstruction;
        }

        bool ExecuteJumpIfTrueOrPop(int jumpTarget)
        {
            var operand = _valStack.Peek();

            if (LogicEvaluator.ShouldShortCircuitOr(ref operand))
            {
                // Leave the truthy value on the stack as the result of the short-circuited `or`
                _callStack.JumpToInstr(jumpTarget);
                return StayAtInstruction;
            }

            _valStack.Pop();
            return GoToNextInstruction;
        }

        bool ExecuteJumpIfFalse(int jumpTarget)
        {
            // Always pops; jumps past the branch body when the condition is false
            var operand = _valStack.Pop();

            if (LogicEvaluator.ShouldShortCircuitAnd(ref operand))
            {
                _callStack.JumpToInstr(jumpTarget);
                return StayAtInstruction;
            }

            return GoToNextInstruction;
        }

        // Unconditional jump: set the instruction pointer and remain there (don't advance).
        bool ExecuteJump(int jumpTarget)
        {
            _callStack.JumpToInstr(jumpTarget);
            return StayAtInstruction;
        }

        #endregion

        #region Variable Operations

        void ExecuteAssignVariable(int operand)
        {
            // Operand -> name via BytecodeChunk; CallStack routes the assign to the current frame's scope.
            var name = _callStack.CurrentBytecodeChunk.GetVariableName(operand);
            var assignVal = _valStack.Pop();

            _callStack.AssignVariableValue(name, ref assignVal);
        }

        void ExecutePushVariableValue(int operand)
        {
            // Operand -> name via BytecodeChunk. Semantic analysis is responsible for ensuring the
            // name exists before this op runs.
            var varName = _callStack.CurrentBytecodeChunk.GetVariableName(operand);

            if (!_callStack.TryGetVariableValue(varName, out var varValue))
            {
                throw new UndefinedNameException(varName, _callStack.CurrentLineNum);
            }

            _valStack.Push(varValue);
        }

        void ExecuteAssignGlobal(int operand)
        {
            var name = _callStack.CurrentBytecodeChunk.GetVariableName(operand);
            var assignVal = _valStack.Pop();

            _callStack.AssignToGlobal(name, ref assignVal);
        }

        void ExecutePushGlobalValue(int operand)
        {
            var varName = _callStack.CurrentBytecodeChunk.GetVariableName(operand);

            if (!_callStack.TryGetGlobal(varName, out var varValue))
            {
                throw new UndefinedNameException(varName, _callStack.CurrentLineNum);
            }

            _valStack.Push(varValue);
        }

        void ExecuteAssignNonLocal(int operand)
        {
            var name = _callStack.CurrentBytecodeChunk.GetVariableName(operand);
            var assignVal = _valStack.Pop();

            _callStack.AssignToNonlocal(name, ref assignVal);
        }

        void ExecutePushNonLocalValue(int operand)
        {
            // Semantic analysis guarantees an enclosing function binding exists; the CallStack
            // helper throws KeyNotFoundException if that invariant is violated.
            var varName = _callStack.CurrentBytecodeChunk.GetVariableName(operand);
            _valStack.Push(_callStack.GetNonlocal(varName));
        }

        #endregion

        #region Attribute Operations

        void ExecuteAssignAttribute(int operand)
        {
            var attrName = _callStack.CurrentBytecodeChunk.GetVariableName(operand);
            _valStack.Pop();
            var target = _valStack.Pop();

            throw new AttributeException(
                DataTypeNames.GetTypeName(target.DataType),
                attrName,
                _callStack.CurrentLineNum);
        }

        void ExecutePushAttribute(int operand)
        {
            var attrName = _callStack.CurrentBytecodeChunk.GetVariableName(operand);
            var target = _valStack.Pop();

            // TODO: class instances add a branch that consults the instance attribute table, then the class method table.
            if (target.DataType == DataType.List)
            {
                var list = target.ToISourceObject();

                if (!list.Directory.Contains(attrName))
                {
                    throw new AttributeException(
                        DataTypeNames.GetTypeName(target.DataType),
                        attrName,
                        _callStack.CurrentLineNum);
                }

                _valStack.Push(list.GetAttribute(new SourceValue(attrName)));
            }
            else if (target.DataType == DataType.Dict)
            {
                // TODO: Create a ToInternalDict and ToInternalList to clean this up
                var dict = target.ToISourceObject();

                if (!dict.Directory.Contains(attrName))
                {
                    throw new AttributeException(
                        DataTypeNames.GetTypeName(target.DataType),
                        attrName,
                        _callStack.CurrentLineNum);
                }

                // TODO: Add implicit overloads to convert strings/longs/ints/doubles/etc to SourceValues
                _valStack.Push(dict.GetAttribute(new SourceValue(attrName)));
            }
            else
            {
                throw new AttributeException(
                    DataTypeNames.GetTypeName(target.DataType),
                    attrName,
                    _callStack.CurrentLineNum);
            }
        }

        #endregion

        #region Collection Operations

        void ExecutePushNewSourceList(int elementCount)
        {
            // Pop N values; reverse so source order is preserved.
            var reversed = new SourceValue[elementCount];

            for (var i = elementCount - 1; i >= 0; i--)
            {
                reversed[i] = _valStack.Pop();
            }

            var list = SourceObjectFactory.CreateNewObject(DataType.List);

            for (var i = 0; i < elementCount; i++)
            {
                list.AppendItem(reversed[i]);
            }

            _valStack.Push(new SourceValue(list));
        }

        void ExecutePushNewSourceDictionary(int pairCount)
        {
            // Pop 2N values (value, key, value, key, ...); rebuild source order before insertion.
            var keys = new SourceValue[pairCount];
            var values = new SourceValue[pairCount];

            for (var i = pairCount - 1; i >= 0; i--)
            {
                values[i] = _valStack.Pop();
                keys[i] = _valStack.Pop();
            }

            var dict = SourceObjectFactory.CreateNewObject(DataType.Dict);

            for (var i = 0; i < pairCount; i++)
            {
                dict.SetItem(keys[i], values[i]);
            }

            _valStack.Push(new SourceValue(dict));
        }

        #endregion

        #region Iterator Operations

        void ExecutePushNewIterator()
        {
            var source = _valStack.Pop();
            var iter = IteratorFactory.GetIterator(source);
            _valStack.Push(new SourceValue(iter));
        }

        bool ExecuteJumpOrIterateFor(int jumpTarget)
        {
            // PeekType the iterator (kept on stack for the whole loop); push next value or jump to
            // exhaust target.
            var iter = (IIterator)_valStack.Peek().ToObject();

            if (iter.TryMoveNext(out var current))
            {
                _valStack.Push(current);
                return GoToNextInstruction;
            }

            _valStack.Pop();
            _callStack.JumpToInstr(jumpTarget);
            return StayAtInstruction;
        }

        #endregion

        #region Subscript Operations

        // TODO: Migrate to SourceObject.SetItem
        void ExecuteAssignSubscript()
        {
            var value = _valStack.Pop();
            var index = _valStack.Pop();
            var target = _valStack.Pop();

            if (target.DataType == DataType.Dict)
            {
                target.ToISourceObject().SetItem(index, value);
            }
            else if (target.DataType == DataType.List)
            {
                if (index.DataType != DataType.Long)
                {
                    throw new DataTypeException(
                        $"list indices must be integers, not {index.DataType}");
                }

                target.ToISourceObject().SetItem(index, value);
            }
            else
            {
                throw new DataTypeException(
                    $"'{DataTypeNames.GetTypeName(target.DataType)}'"
                    + " object does not support item assignment");
            }
        }

        // TODO: Migrate to SourceObject.SetItem
        void ExecutePushSubscriptValue()
        {
            var index = _valStack.Pop();
            var target = _valStack.Pop();

            // TODO: BinaryAdd a branch here for strings.
            if (target.DataType == DataType.Dict)
            {
                try
                {
                    _valStack.Push(target.ToISourceObject().GetItem(index));
                }
                catch (SubscriptException ex)
                {
                    throw new SubscriptException(ex.KeyRepr, _callStack.CurrentLineNum);
                }
            }
            else if (target.DataType == DataType.List)
            {
                if (index.DataType != DataType.Long)
                {
                    throw new DataTypeException(
                        $"list indices must be integers, not {index.DataType}");
                }

                _valStack.Push(target.ToISourceObject().GetItem(index));
            }
            else
            {
                throw new DataTypeException(
                    $"'{DataTypeNames.GetTypeName(target.DataType)}' object is not subscriptable");
            }
        }

        void ExecutePushSubscriptSliceValue()
        {
            var step = _valStack.Pop();
            var stop = _valStack.Pop();
            var start = _valStack.Pop();
            var target = _valStack.Pop();

            // FUTURE: strings add a parallel slice branch.
            if (target.DataType != DataType.List)
            {
                throw new DataTypeException($"'{target.DataType}' object is not subscriptable");
            }

            var slice = new SourceValue(new SourceSlice(start, stop, step));
            _valStack.Push(target.ToISourceObject().GetItem(slice));
        }

        #endregion

        #region Function Call Operations

        bool ExecuteCallFunction(int argCount)
        {
            var args = new SourceValue[argCount];

            for (var i = argCount - 1; i >= 0; i--)
            {
                args[i] = _valStack.Pop();
            }

            var calleeValue = _valStack.Pop();

            // If the SourceValue is storing a closure inside (i.e., a function made up of bytecode)
            if (IsClosure(calleeValue))
            {
                // Switches to the closure's frame, so Execute will next execute the first
                // instruction of the closure's bytecodeChunk.
                PushClosureStackFrame(argCount, calleeValue.ToISourceObject(), args);
                return StayAtInstruction;
            }

            // Will push its return value onto the stack.
            CallInteropFunction(calleeValue, args);
            return GoToNextInstruction;
        }

        static bool IsClosure(SourceValue calleeValue)
        {
            return calleeValue.DataType == DataType.Function;
        }

        void CallInteropFunction(SourceValue calleeValue, SourceValue[] args)
        {
            var toObjVal = calleeValue.ToObject();

            switch (toObjVal)
            {
                case Action action:
                    ThrowIfArguments(calleeValue, args);
                    action.Invoke();
                    break;
                // TODO: Find a way around this dependency
                case Func<object> funcNoParams:
                    ThrowIfArguments(calleeValue, args);
                    _valStack.Push(new SourceValue(funcNoParams.Invoke()));
                    break;
                case Action<object> actionOneObjectParam:
                    ThrowIfArgumentCount(calleeValue, args, 1);
                    actionOneObjectParam.Invoke(args[0].ToObject());
                    break;
                case Action<object[]> actionObjectArrayParam:
                    actionObjectArrayParam.Invoke(SourceValue.ToObjects(args ?? Array.Empty<SourceValue>()));
                    break;
                case Func<object[], object> funcParams:
                    _valStack.Push(
                        new SourceValue(funcParams.Invoke(SourceValue.ToObjects(args ?? Array.Empty<SourceValue>()))));

                    break;
                default:
                    // TODO: After built-ins refactor, remove this
                    _valStack.Push(calleeValue.InvokeHostDelegate(args));
                    break;
            }
        }

        static void ThrowIfArguments(SourceValue calleeValue, SourceValue[] args)
        {
            if (args != null && args.Length != 0)
            {
                throw new ArgumentException(
                    $"'{calleeValue.DataType}' does not support arguments");
            }
        }

        static void ThrowIfArgumentCount(SourceValue calleeValue, SourceValue[] args, int expectedArgsCount)
        {
            var actualArgsCount = args?.Length ?? 0;
            if (actualArgsCount != expectedArgsCount)
            {
                throw new ArgumentException(
                    $"'{calleeValue.DataType}' expects {expectedArgsCount} argument(s), got {actualArgsCount}");
            }
        }

        void PushClosureStackFrame(int argCount, ISourceObject function, SourceValue[] args)
        {
            // Re-push args; function body's first ops are param-bind AssignLocal's, popping right-to-left.
            for (var i = 0; i < argCount; i++)
            {
                _valStack.Push(args[i]);
            }

            // Advance caller's IP BEFORE pushing the frame, so PushReturnValue lands at the next
            // caller instruction.
            _callStack.MoveToNextInstruction();
            _callStack.EnterFunctionCall(function, argCount);
        }

        void ExecutePushNewSourceFunction()
        {
            // Type guaranteed to be at top of stack
            var template = (FunctionDefinition)_valStack.Pop().ToObject();
            var closure = template.MakeClosure(_callStack.CurrentScope);

            _valStack.Push(new SourceValue(closure));
        }

        #endregion
    }
}
