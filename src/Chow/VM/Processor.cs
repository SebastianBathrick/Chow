using System;
using System.Collections.Generic;
using Chow.Bytecode;
using Chow.Objects;
using Chow.VM.Utilities;

namespace Chow.VM
{
    sealed class Processor
    {
        const bool GoToNextInstruction = true;
        const bool StayAtInstruction = false;

        readonly CallStack _callStack;
        readonly Stack<SourceValue> _valStack;
        SourceValue _expressionStatementVal = SourceValue.None;

        Instruction CurrentOperation => _callStack.CurrentInstr;

        // Chunk is null when the client is exclusively calling a closure
        public Processor(Scope globalScope = null, Chunk chunk = null)
        {
            _callStack = new CallStack(chunk ?? new Chunk(), globalScope);
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
            switch (CurrentOperation.Code)
            {
                case OperationCode.Pop:
                    _valStack.Pop();
                    break;
                case OperationCode.PushConstantValue:
                    _valStack.Push(_callStack.CurrentChunk.ReadConstant(CurrentOperation.Operand));
                    break;

                // -- Binary Operations------------------------------------------------------------
                case OperationCode.BinaryAdd:
                    _valStack.Push(ArithmeticEvaluator.Add(
                        r: _valStack.Pop(), l: _valStack.Pop()));
                    break;
                case OperationCode.BinarySubtract:
                    _valStack.Push(ArithmeticEvaluator.Subtract(
                        r: _valStack.Pop(), l: _valStack.Pop()));
                    break;
                case OperationCode.BinaryMultiply:
                    _valStack.Push(ArithmeticEvaluator.Multiply(
                        r: _valStack.Pop(), l: _valStack.Pop()));
                    break;
                case OperationCode.BinaryDivide:
                    _valStack.Push(ArithmeticEvaluator.Divide(
                        r: _valStack.Pop(), l: _valStack.Pop()));
                    break;
                case OperationCode.BinaryModulus:
                    _valStack.Push(ArithmeticEvaluator.Pow(
                        r: _valStack.Pop(), l: _valStack.Pop()));
                    break;
                case OperationCode.BinaryPow:
                    _valStack.Push(ArithmeticEvaluator.EvaluateExponent(
                        r: _valStack.Pop(), l: _valStack.Pop()));
                    break;
                case OperationCode.BinaryFloor:
                    _valStack.Push(ArithmeticEvaluator.EvaluateFloorDivision(
                        r: _valStack.Pop(), l: _valStack.Pop()));
                    break;
                case OperationCode.BinaryEqual:
                    _valStack.Push(ComparisonEvaluator.EvaluateEqual(
                        r: _valStack.Pop(), l: _valStack.Pop()));
                    break;
                case OperationCode.BinaryNotEqual:
                    _valStack.Push(ComparisonEvaluator.EvaluateNotEqual(
                        r: _valStack.Pop(), l: _valStack.Pop()));
                    break;
                case OperationCode.BinaryLess:
                    _valStack.Push(ComparisonEvaluator.EvaluateLess(
                        r: _valStack.Pop(), l: _valStack.Pop()));
                    break;
                case OperationCode.BinaryGreater:
                    _valStack.Push(ComparisonEvaluator.EvaluateGreater(
                        r: _valStack.Pop(), l: _valStack.Pop()));
                    break;
                case OperationCode.BinaryLessEqual:
                    _valStack.Push(ComparisonEvaluator.EvaluateLessEqual(
                        r: _valStack.Pop(), l: _valStack.Pop()));
                    break;
                case OperationCode.BinaryGreaterEqual:
                    _valStack.Push(ComparisonEvaluator.EvaluateGreaterEqual(
                        r: _valStack.Pop(), l: _valStack.Pop()));
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
                    _valStack.Push(LogicEvaluator.EvaluateNot(_valStack.Pop()));
                    break;
                case OperationCode.UnaryNegate:
                    _valStack.Push(ArithmeticEvaluator.EvaluateNegation(_valStack.Pop()));
                    break;

                // -- Control Structure Operations ------------------------------------------------
                case OperationCode.JumpIfFalseOrPop:
                    return ExecuteJumpIfFalseOrPop();
                case OperationCode.JumpIfTrueOrPop:
                    return ExecuteJumpIfTrueOrPop();
                case OperationCode.JumpIfFalse:
                    return ExecuteJumpIfFalse();
                case OperationCode.JumpPastElseBranches:
                    return ExecuteJump();
                case OperationCode.JumpToLoopStart:
                    return ExecuteJump();

                // -- Variable Operations ---------------------------------------------------------
                case OperationCode.AssignVariable:
                    ExecuteAssignVariable();
                    break;
                case OperationCode.PushVariableValue:
                    ExecutePushVariableValue();
                    break;
                case OperationCode.AssignGlobal:
                    ExecuteAssignGlobal();
                    break;
                case OperationCode.PushGlobalValue:
                    ExecutePushGlobalValue();
                    break;
                case OperationCode.AssignNonLocal:
                    ExecuteAssignNonLocal();
                    break;
                case OperationCode.PushNonLocalValue:
                    ExecutePushNonLocalValue();
                    break;

                // -- Attribute Operations --------------------------------------------------------
                case OperationCode.AssignAttribute:
                    ExecuteAssignAttribute();
                    break;
                case OperationCode.PushAttributeValue:
                    ExecutePushAttribute();
                    break;

                // -- Collection Operations -------------------------------------------------------
                case OperationCode.PushNewSourceList:
                    ExecutePushNewSourceList(CurrentOperation.Operand);
                    break;
                case OperationCode.PushNewSourceDictionary:
                    ExecutePushNewSourceDictionary(CurrentOperation.Operand);
                    break;

                // -- Iterator Operations ---------------------------------------------------------
                case OperationCode.PushNewIteratorWithValue:
                    ExecutePushNewIterator();
                    break;
                case OperationCode.JumpOrForIteratorNext:
                    return ExecuteJumpOrIterateFor();

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
                    // If false, the chunk will have switched to a SourceFunction's
                    return ExecuteCallFunction(CurrentOperation.Operand);
                case OperationCode.PushReturnValue:
                    _callStack.ExitFunctionCall();
                    return StayAtInstruction;
                case OperationCode.PushNewSourceFunction:
                    ExecutePushNewSourceFunction();
                    break;

                // -- Expression Evaluation Operations --------------------------------------------
                case OperationCode.CoerceToStr:
                    EvaluateCoerceToStr();
                    break;
                case OperationCode.PopExpressionStatementResult:
                    _expressionStatementVal = _valStack.Pop();
                    break;

                default:
                    throw new NotImplementedException($"Execution of {CurrentOperation.Code} is not implemented.");
            }

            return GoToNextInstruction;
        }

        /// <summary>Calls a function stored in a global variable with the name provided.</summary>
        /// <param name="callVarName">The name of a variable declared in the global scope. Caller
        /// is responsible for verifying the name is defined.</param>
        /// <param name="args">The arguments to pass to the function. If there aren't any, this
        /// parameter can be null.</param>
        /// <returns>The result of the function call.</returns>
        /// <remarks>Assumes that there is a global scope already set up that was provided to the
        /// constructor.</remarks>
        public SourceValue CallGlobalFunction(string callVarName, SourceValue[] args)
        {
            _valStack.Push(_callStack.GetVariableValue(callVarName));

            if (args != null)
            {
                for (var i = 0; i < args.Length; i++)
                {
                    _valStack.Push(args[i]);
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
            // TODO: Add bitwise ors to the LogicEvaluator
            var r = _valStack.Pop();
            var l = _valStack.Pop();
            _valStack.Push(l.CreateUnion(r));
        }

        void EvaluateIn(bool negate)
        {
            var container = _valStack.Pop();
            var needle = _valStack.Pop();
            var found = false;

            if (container.DataType == DataType.Dict)
            {
                found = ((SourceDictionary)container.ToObject()).ContainsKey(needle);
            }
            else if (container.DataType == DataType.List)
            {
                var list = (SourceList)container.ToObject();

                for (var i = 0; i < list.Count && !found; i++)
                {
                    found = list[i].IsTypeAgnosticEqualTo(needle);
                }
            }
            else
            {
                throw new DataTypeException($"argument of type '{container.DataType}' is not iterable");
            }

            _valStack.Push(new SourceValue(negate ? !found : found));
        }

        #endregion

        #region Control Structure Operations

        bool ExecuteJumpIfFalseOrPop()
        {
            var operand = _valStack.Peek();

            if (LogicEvaluator.ShouldShortCircuitAnd(ref operand))
            {
                // Leave the falsy value on the stack as the result of the short-circuited `and`
                _callStack.JumpToInstr(CurrentOperation.Operand);
                return StayAtInstruction;
            }

            _valStack.Pop();
            return GoToNextInstruction;
        }

        bool ExecuteJumpIfTrueOrPop()
        {
            var operand = _valStack.Peek();

            if (LogicEvaluator.ShouldShortCircuitOr(ref operand))
            {
                // Leave the truthy value on the stack as the result of the short-circuited `or`
                _callStack.JumpToInstr(CurrentOperation.Operand);
                return StayAtInstruction;
            }

            _valStack.Pop();
            return GoToNextInstruction;
        }

        bool ExecuteJumpIfFalse()
        {
            // Always pops; jumps past the branch body when the condition is false
            var operand = _valStack.Pop();

            if (LogicEvaluator.ShouldShortCircuitAnd(ref operand))
            {
                _callStack.JumpToInstr(CurrentOperation.Operand);
                return StayAtInstruction;
            }

            return GoToNextInstruction;
        }

        // Unconditional jump: set the instruction pointer and remain there (don't advance).
        bool ExecuteJump()
        {
            _callStack.JumpToInstr(CurrentOperation.Operand);
            return StayAtInstruction;
        }

        #endregion

        #region Variable Operations

        void ExecuteAssignVariable()
        {
            // Operand -> name via Chunk; CallStack routes the assign to the current frame's scope.
            var name = _callStack.CurrentChunk.ReadVariableName(CurrentOperation.Operand);
            var assignVal = _valStack.Pop();

            _callStack.AssignVariableValue(name, assignVal);
        }

        void ExecutePushVariableValue()
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

        void ExecuteAssignGlobal()
        {
            var name = _callStack.CurrentChunk.ReadVariableName(CurrentOperation.Operand);
            var assignVal = _valStack.Pop();

            _callStack.AssignToGlobal(name, assignVal);
        }

        void ExecutePushGlobalValue()
        {
            var varName = _callStack.CurrentChunk.ReadVariableName(CurrentOperation.Operand);

            if (!_callStack.IsGlobalDefined(varName))
            {
                throw new UndefinedNameException(varName, GetCurrentLineNumber());
            }

            _valStack.Push(_callStack.GetGlobal(varName));
        }

        void ExecuteAssignNonLocal()
        {
            var name = _callStack.CurrentChunk.ReadVariableName(CurrentOperation.Operand);
            var assignVal = _valStack.Pop();

            _callStack.AssignToNonlocal(name, assignVal);
        }

        void ExecutePushNonLocalValue()
        {
            // Semantic analysis guarantees an enclosing function binding exists; the CallStack
            // helper throws KeyNotFoundException if that invariant is violated.
            var varName = _callStack.CurrentChunk.ReadVariableName(CurrentOperation.Operand);
            _valStack.Push(_callStack.GetNonlocal(varName));
        }

        #endregion

        #region Attribute Operations

        void ExecuteAssignAttribute()
        {
            var attrName = _callStack.CurrentChunk.ReadVariableName(CurrentOperation.Operand);
            _valStack.Pop();
            var target = _valStack.Pop();

            throw new AttributeException(ParseDataTypeName(target.DataType), attrName, GetCurrentLineNumber());
        }

        void ExecutePushAttribute()
        {
            var attrName = _callStack.CurrentChunk.ReadVariableName(CurrentOperation.Operand);
            var target = _valStack.Pop();

            // TODO: class instances add a branch that consults the instance attribute table, then the class method table.
            if (target.DataType == DataType.List)
            {
                var list = (SourceList)target.ToObject();

                if (!list.HasMethod(attrName))
                {
                    throw new AttributeException(ParseDataTypeName(target.DataType), attrName, GetCurrentLineNumber());
                }

                _valStack.Push(list[attrName]);
            }
            else if (target.DataType == DataType.Dict)
            {
                // TODO: Create a ToInternalDict and ToInternalList to clean this up
                var dict = (SourceDictionary)target.ToObject();

                if (!dict.HasMethod(attrName))
                {
                    throw new AttributeException(ParseDataTypeName(target.DataType), attrName, GetCurrentLineNumber());
                }

                _valStack.Push(dict[attrName]);
            }
            else
            {
                throw new AttributeException(ParseDataTypeName(target.DataType), attrName, GetCurrentLineNumber());
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

            var list = new SourceList();

            for (var i = 0; i < elementCount; i++)
            {
                list.Add(reversed[i]);
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

            var dict = new SourceDictionary();

            for (var i = 0; i < pairCount; i++)
            {
                dict.Add(keys[i], values[i]);
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

        bool ExecuteJumpOrIterateFor()
        {
            // Peek the iterator (kept on stack for the whole loop); push next value or jump to exhaust target.
            var iter = (IIterator)_valStack.Peek().ToObject();

            if (iter.TryMoveNext(out var current))
            {
                _valStack.Push(current);
                return GoToNextInstruction;
            }

            _valStack.Pop();
            _callStack.JumpToInstr(CurrentOperation.Operand);
            return StayAtInstruction;
        }

        #endregion

        #region Subscript Operations

        void ExecuteAssignSubscript()
        {
            var value = _valStack.Pop();
            var index = _valStack.Pop();
            var target = _valStack.Pop();

            if (target.DataType == DataType.Dict)
            {
                ((SourceDictionary)target.ToObject())[index] = value;
            }
            else if (target.DataType == DataType.List)
            {
                if (index.DataType != DataType.Long)
                {
                    throw new DataTypeException($"list indices must be integers, not {index.DataType}");
                }

                ((SourceList)target.ToObject())[(int)index.ToLong()] = value;
            }
            else
            {
                throw new DataTypeException(
                    $"'{ParseDataTypeName(target.DataType)}' object does not support item assignment");
            }
        }

        void ExecutePushSubscriptValue()
        {
            var index = _valStack.Pop();
            var target = _valStack.Pop();

            // TODO: BinaryAdd a branch here for strings.
            if (target.DataType == DataType.Dict)
            {
                try
                {
                    _valStack.Push(((SourceDictionary)target.ToObject())[index]);
                }
                catch (SubscriptException ex)
                {
                    throw new SubscriptException(ex.KeyRepr, GetCurrentLineNumber());
                }
            }
            else if (target.DataType == DataType.List)
            {
                if (index.DataType != DataType.Long)
                {
                    throw new DataTypeException($"list indices must be integers, not {index.DataType}");
                }

                _valStack.Push(((SourceList)target.ToObject())[(int)index.ToLong()]);
            }
            else
            {
                throw new DataTypeException(
                    $"'{ParseDataTypeName(target.DataType)}' object is not subscriptable");
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

            _valStack.Push(((SourceList)target.ToObject()).GetSlice(start, stop, step));
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
                // instruction of the closure's chunk.
                PushClosureStackFrame(argCount, (SourceFunction)calleeValue.ToObject(), args);
                return StayAtInstruction;
            }

            // Will push its return value onto the stack.
            CallInteropFunction(calleeValue, args);
            return GoToNextInstruction;
        }

        static bool IsClosure(SourceValue calleeValue)
        {
            return calleeValue.DataType == DataType.Object && calleeValue.ToObject() is SourceFunction;
        }

        void CallInteropFunction(SourceValue calleeValue, SourceValue[] args)
        {
            _valStack.Push(calleeValue.InvokeHostDelegate(args));
        }

        void PushClosureStackFrame(int argCount, SourceFunction sourceFunction, SourceValue[] args)
        {
            if (argCount != sourceFunction.ParamCount)
            {
                throw new DataTypeException($"{sourceFunction.Name}() takes {sourceFunction.ParamCount} positional arguments but {argCount} were given");
            }

            // Re-push args; function body's first ops are param-bind AssignVariable's, popping right-to-left.
            for (var i = 0; i < argCount; i++)
            {
                _valStack.Push(args[i]);
            }

            // Advance caller's IP BEFORE pushing the frame, so PushReturnValue lands at the next caller instruction.
            _callStack.MoveToNextInstruction();
            _callStack.EnterFunctionCall(sourceFunction);
        }

        void ExecutePushNewSourceFunction()
        {
            // Type guaranteed to be at top of stack
            var template = (FunctionDefinition)_valStack.Pop().ToObject();

            var captured = _callStack.CurrentScope;
            var closure = new SourceFunction(template.Chunk, captured, template.Name, template.ParamCount);

            _valStack.Push(new SourceValue(closure));
        }

        #endregion

        #region Expression Evaluation Operations

        void EvaluateCoerceToStr()
        {
            var operand = _valStack.Pop();
            _valStack.Push(operand.CreateStr());
        }

        #endregion

        #region Helper Methods

        static string ParseDataTypeName(DataType dataType)
        {
            // TODO: Refactor so there's a single source of truth for datatype names used in error messages
            return dataType.ToString().ToLowerInvariant();
        }

        // TODO: Refactor to get rid of this method, as Processor no longer indexes the instruction stream directly (CallStack does)
        int GetCurrentLineNumber()
        {
            return _callStack.CurrentLineNum;
        }

        #endregion

    }
}
