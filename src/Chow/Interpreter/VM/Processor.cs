using System;
using System.Collections.Generic;
using Chow.Bytecode;
using Chow.Code;
using Chow.Interpreter.Exceptions;
using Chow.SourceData;

namespace Chow.Interpreter.VM
{
    sealed class Processor
    {
        const bool GoToNextInstruction = true;
        const bool StayAtInstruction = false;

        const string NoInitializerArityErrorFormat = "{0}() takes no arguments but {1} were given";

        readonly CallStack _callStack;
        readonly Stack<SourceValue> _valStack;
        SourceValue _exprStmntVal = SourceValue.None;

        // BytecodeChunk is null when the client is exclusively calling a closure
        public Processor(Scope globalScope = null, BytecodeChunk bytecodeChunk = null)
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

            return _exprStmntVal;
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
                    ExecutePushNewSourceDict(instr.Operand);
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
                    ExecuteReturn();
                    return StayAtInstruction;
                case OperationCode.PushNewSourceFunction:
                    ExecutePushNewSourceFunction();
                    break;
                case OperationCode.PushNewSourceClass:
                    ExecutePushNewSourceClass(instr.Operand);
                    break;

                // -- Expression Evaluation Operations --------------------------------------------
                case OperationCode.CoerceToStr:
                    _valStack.Push(new SourceValue(_valStack.Pop().ToString()));
                    break;
                case OperationCode.PopExpressionStatementResult:
                    _exprStmntVal = _valStack.Count != 0 ? _valStack.Pop() : SourceValue.None;
                    break;
                default:
                    throw new NotImplementedException($"Execution of {instr.Code} is not implemented.");
            }

            return GoToNextInstruction;
        }

        /// <summary>
        /// Calls <paramref name="callee"/> and runs it to completion, as though a
        /// <c>CallFunction</c> instruction had been reached. Lets the host invoke a Chow callable
        /// without a surrounding bytecode chunk to sit inside.
        /// </summary>
        /// <param name="callee">
        /// The value to call: a Chow closure, a bound method, a class, or a host delegate. Whether
        /// it is callable at all is decided by the same logic that runs a call in compiled code, so
        /// a non-callable raises the language's own error.
        /// </param>
        /// <param name="args">
        /// The arguments to pass to the call. If there aren't any, this parameter can be null.
        /// </param>
        /// <returns>The result of the call, or None when the callee produced no value.</returns>
        public SourceValue CallValue(SourceValue callee, SourceValue[] args)
        {
            // Void host delegates push no result, so the depth recorded here is what says whether
            // one ever arrived.
            var stackDepthBeforeCall = _valStack.Count;
            var argCount = args?.Length ?? 0;

            _valStack.Push(callee);

            for (var i = 0; i < argCount; i++)
            {
                _valStack.Push(args[i]);
            }

            // A closure switches to its own frame rather than producing a value inline, so it has to
            // be run to its return before the result is on the stack.
            if (ExecuteCallFunction(argCount) == StayAtInstruction)
            {
                Execute();
            }

            return _valStack.Count > stackDepthBeforeCall ? _valStack.Pop() : SourceValue.None;
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

            if (!LogicEvaluator.ShouldShortCircuitAnd(ref operand))
            {
                return GoToNextInstruction;
            }

            _callStack.JumpToInstr(jumpTarget);
            return StayAtInstruction;

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
            var value = _valStack.Pop();
            var target = _valStack.Pop();

            // Classes and their instances are the only types with a writable attribute table;
            // everything else rejects assignment, as it does in Python.
            switch (target.DataType)
            {
                case DataType.Instance:
                case DataType.Class:
                    target.ToISourceObject().SetAttribute(attrName, value);
                    break;
                default:
                    throw new AttributeException(
                        DataTypeNames.GetTypeName(target.DataType),
                        attrName,
                        _callStack.CurrentLineNum);
            }
        }

        void ExecutePushAttribute(int operand)
        {
            var attrName = _callStack.CurrentBytecodeChunk.GetVariableName(operand);
            var target = _valStack.Pop();

            switch (target.DataType)
            {
                case DataType.Instance:
                {
                    // Pre-checked rather than letting the object throw, so the error carries the
                    // line number the access occurred on.
                    var instance = (SourceClassInstance)target.ToISourceObject();

                    if (!instance.TryGetAttribute(attrName, out var attrValue))
                    {
                        throw new AttributeException(
                            instance.TypeName,
                            attrName,
                            _callStack.CurrentLineNum);
                    }

                    _valStack.Push(attrValue);
                    break;
                }
                case DataType.Class:
                {
                    var sourceClass = (SourceClass)target.ToISourceObject();

                    if (!sourceClass.TryGetAttribute(attrName, out var attrValue))
                    {
                        throw new AttributeException(
                            sourceClass.TypeName,
                            attrName,
                            _callStack.CurrentLineNum);
                    }

                    _valStack.Push(attrValue);
                    break;
                }
                case DataType.List:
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
                    break;
                }
                case DataType.Dict:
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
                    break;
                }
                default:
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

        void ExecutePushNewSourceDict(int pairCount)
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

            switch (target.DataType)
            {
                case DataType.Dict:
                    target.ToISourceObject().SetItem(index, value);
                    break;
                case DataType.List when index.DataType != DataType.Long:
                    throw new DataTypeException(
                        $"list indices must be integers, not {index.DataType}");
                case DataType.List:
                    target.ToISourceObject().SetItem(index, value);
                    break;
                default:
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

            switch (target.DataType)
            {
                // TODO: BinaryAdd a branch here for strings.
                case DataType.Dict:
                    try
                    {
                        _valStack.Push(target.ToISourceObject().GetItem(index));
                    }
                    catch (SubscriptException ex)
                    {
                        throw new SubscriptException(ex.KeyRepr, _callStack.CurrentLineNum);
                    }

                    break;
                case DataType.List when index.DataType != DataType.Long:
                    throw new DataTypeException(
                        $"list indices must be integers, not {index.DataType}");
                case DataType.List:
                    _valStack.Push(target.ToISourceObject().GetItem(index));
                    break;
                default:
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
                // Switches to the closure's frame, so Run will next execute the first
                // instruction of the closure's bytecodeChunk.
                PushClosureStackFrame(argCount, calleeValue.ToISourceObject(), args);
                return StayAtInstruction;
            }

            // Calling a class constructs an instance of it.
            if (calleeValue.DataType == DataType.Class)
            {
                return ExecuteConstructInstance((SourceClass)calleeValue.ToISourceObject(), args);
            }

            // Will push its return value onto the stack.
            CallInteropFunction(calleeValue, args);
            return GoToNextInstruction;
        }

        static bool IsClosure(SourceValue calleeValue)
        {
            return calleeValue.DataType == DataType.Function;
        }

        // TODO: Refactor and move to a different class (thus, avoiding ChowObject dependency)
        void CallInteropFunction(SourceValue calleeValue, SourceValue[] args)
        {
            var toObjVal = calleeValue.ToObject();

            switch (toObjVal)
            {
                case Action action:
                    ThrowIfArguments(calleeValue, args);
                    action.Invoke();
                    break;
                // Return-type covariance makes a Func<ChowObject> match case Func<object>, so it has
                // to be tested first. Parameter contravariance runs the other way, which is why the
                // Action<object> cases stay ahead of their Action<ChowObject> counterparts.
                case Func<ChowObject> funcNoChowParams:
                    ThrowIfArguments(calleeValue, args);
                    _valStack.Push(ToSourceValue(funcNoChowParams.Invoke()));
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
                case Action<object, object> actionTwoObjectParams:
                    ThrowIfArgumentCount(calleeValue, args, 2);
                    actionTwoObjectParams.Invoke(args[0].ToObject(), args[1].ToObject());
                    break;
                case Action<object, object, object> actionThreeObjectParams:
                    ThrowIfArgumentCount(calleeValue, args, 3);
                    actionThreeObjectParams.Invoke(
                        args[0].ToObject(),
                        args[1].ToObject(),
                        args[2].ToObject());
                    break;
                case Func<object, object> funcOneObjectParam:
                    ThrowIfArgumentCount(calleeValue, args, 1);
                    _valStack.Push(new SourceValue(funcOneObjectParam.Invoke(args[0].ToObject())));
                    break;
                case Func<object, object, object> funcTwoObjectParams:
                    ThrowIfArgumentCount(calleeValue, args, 2);
                    _valStack.Push(
                        new SourceValue(funcTwoObjectParams.Invoke(args[0].ToObject(), args[1].ToObject())));
                    break;
                case Func<object, object, object, object> funcThreeObjectParams:
                    ThrowIfArgumentCount(calleeValue, args, 3);
                    _valStack.Push(
                        new SourceValue(
                            funcThreeObjectParams.Invoke(
                                args[0].ToObject(),
                                args[1].ToObject(),
                                args[2].ToObject())));
                    break;
                case Action<object[]> actionObjectArrayParam:
                    actionObjectArrayParam.Invoke(SourceValue.ToObjects(args ?? Array.Empty<SourceValue>()));
                    break;
                case Func<object[], object> funcParams:
                    _valStack.Push(
                        new SourceValue(funcParams.Invoke(SourceValue.ToObjects(args ?? Array.Empty<SourceValue>()))));

                    break;
                case Action<ChowObject> actionOneChowParam:
                    ThrowIfArgumentCount(calleeValue, args, 1);
                    actionOneChowParam.Invoke(ApiConverter.ConvertToClass(args[0]));
                    break;
                case Action<ChowObject, ChowObject> actionTwoChowParams:
                    ThrowIfArgumentCount(calleeValue, args, 2);
                    actionTwoChowParams.Invoke(
                        ApiConverter.ConvertToClass(args[0]),
                        ApiConverter.ConvertToClass(args[1]));
                    break;
                case Action<ChowObject, ChowObject, ChowObject> actionThreeChowParams:
                    ThrowIfArgumentCount(calleeValue, args, 3);
                    actionThreeChowParams.Invoke(
                        ApiConverter.ConvertToClass(args[0]),
                        ApiConverter.ConvertToClass(args[1]),
                        ApiConverter.ConvertToClass(args[2]));
                    break;
                case Func<ChowObject, ChowObject> funcOneChowParam:
                    ThrowIfArgumentCount(calleeValue, args, 1);
                    _valStack.Push(
                        ToSourceValue(funcOneChowParam.Invoke(ApiConverter.ConvertToClass(args[0]))));
                    break;
                case Func<ChowObject, ChowObject, ChowObject> funcTwoChowParams:
                    ThrowIfArgumentCount(calleeValue, args, 2);
                    _valStack.Push(
                        ToSourceValue(
                            funcTwoChowParams.Invoke(
                                ApiConverter.ConvertToClass(args[0]),
                                ApiConverter.ConvertToClass(args[1]))));
                    break;
                case Func<ChowObject, ChowObject, ChowObject, ChowObject> funcThreeChowParams:
                    ThrowIfArgumentCount(calleeValue, args, 3);
                    _valStack.Push(
                        ToSourceValue(
                            funcThreeChowParams.Invoke(
                                ApiConverter.ConvertToClass(args[0]),
                                ApiConverter.ConvertToClass(args[1]),
                                ApiConverter.ConvertToClass(args[2]))));
                    break;
                case Action<ChowObject[]> actionChowArrayParam:
                    actionChowArrayParam.Invoke(ApiConverter.ConvertToClass(args));
                    break;
                case Func<ChowObject[], ChowObject> funcChowParams:
                    _valStack.Push(ToSourceValue(funcChowParams.Invoke(ApiConverter.ConvertToClass(args))));
                    break;
                default:
                    // TODO: After built-ins refactor, remove this
                    _valStack.Push(calleeValue.InvokeHostDelegate(args));
                    break;
            }
        }

        // A host delegate returning null is treated as Chow's None, matching ChowObject's delegate
        // conversion operators.
        static SourceValue ToSourceValue(ChowObject result)
        {
            return result is null ? SourceValue.None : ApiConverter.Convert(result);
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

        /// <summary>
        /// Builds an instance of <paramref name="sourceClass"/> and runs its constructor, if it
        /// declares one.
        /// </summary>
        /// <returns>
        /// Whether to advance past the call instruction. A class with a constructor enters that
        /// frame instead, so execution stays put.
        /// </returns>
        bool ExecuteConstructInstance(SourceClass sourceClass, SourceValue[] args)
        {
            var instance = new SourceValue(new SourceClassInstance(sourceClass));

            if (!sourceClass.TryGetInitializer(out var initializer))
            {
                if (args.Length != 0)
                {
                    throw new DataTypeException(
                        string.Format(NoInitializerArityErrorFormat, sourceClass.Name, args.Length));
                }

                _valStack.Push(instance);
                return GoToNextInstruction;
            }

            PushClosureStackFrame(args.Length, initializer.Bind(instance), args);

            // The constructor returns None, so the frame carries the instance forward as what this
            // call site evaluates to.
            _callStack.SetConstructionResult(instance);
            return StayAtInstruction;
        }

        void ExecuteReturn()
        {
            var completedFrame = _callStack.ExitFunctionCall();

            if (!completedFrame.HasConstructionResult)
            {
                return;
            }

            // Discard what __init__ returned; `Point(1, 2)` evaluates to the instance itself.
            _valStack.Pop();
            _valStack.Push(completedFrame.ConstructionResult);
        }

        void PushClosureStackFrame(int argCount, ISourceObject function, SourceValue[] args)
        {
            // A bound method's receiver goes on first so it lands in the first parameter: the body
            // binds params in reverse, popping the last one off the top.
            if (function is SourceFunction sourceFunc && sourceFunc.HasReceiver)
            {
                _valStack.Push(sourceFunc.Receiver);
            }

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

        void ExecutePushNewSourceClass(int classVarCount)
        {
            // The definition is pushed last, so it sits above the class-variable values.
            var template = (ClassDefinition)_valStack.Pop().ToObject();

            // Pop N values; reverse so declaration order is preserved.
            var classVarValues = new SourceValue[classVarCount];

            for (var i = classVarCount - 1; i >= 0; i--)
            {
                classVarValues[i] = _valStack.Pop();
            }

            var sourceClass = template.MakeClass(_callStack.CurrentScope, classVarValues);
            _valStack.Push(new SourceValue(sourceClass));
        }

        #endregion
    }
}
