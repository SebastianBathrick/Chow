using System;
using System.Collections.Generic;
using Chow.Bytecode;
using Chow.Objects;
using Chow.VM.Utilities;
namespace Chow.VM
{
    sealed class Processor
    {

        #region Fields

        readonly Objects.Scope _globalScope;
        readonly CallStack _callStack;
        readonly Stack<SourceValue> _valStack;

        #endregion

        #region Properties

        Instruction CurrentOperation => _callStack.CurrentInstr;

        #endregion

        #region Constructors

        public Processor(Chunk chunk, Objects.Scope globalScope)
            : this(globalScope, chunk)
        {
        }

        // Chunk is null when the client is exclusively calling a closure
        public Processor(Objects.Scope globalScope = null, Chunk chunk = null)
        {
            // TODO: Update tests so that this does not throw. Processor no longer
            // instantiates its own global scope; the caller is responsible for that
            _globalScope = globalScope;
            _callStack = new CallStack(chunk ?? new Chunk(), _globalScope);
            _valStack = new Stack<SourceValue>();
        }

        #endregion

        #region Public API

        public SourceValue Execute()
        {
            var lastExprStmntValue = SourceValue.None;

            while (_callStack.IsInstructionToExecute)
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
                            // Do not advance the instruction pointer, because this is not the
                            // same Chunk this iteration started with. Now the pointer refers to
                            // the closure's Chunk instruction pointer and pointed at its first
                            // instruction. Use continue so the first instruction is evaluated
                            // and not skipped.
                            continue;
                        }

                        // An interop call would have already returned and this is the same Chunk
                        // this iteration started with, and no function Chunk instructions need to
                        // be evaluated.
                        break;
                    }

                    #region Binary Operators

                    case OperationCode.Add:
                    case OperationCode.Subtract:
                    case OperationCode.Multiply:
                    case OperationCode.Divide:
                    case OperationCode.Modulus:
                    case OperationCode.Exponentiate:
                    case OperationCode.FloorDivide:
                    case OperationCode.Equal:
                    case OperationCode.NotEqual:
                    case OperationCode.Less:
                    case OperationCode.Greater:
                    case OperationCode.LessEqual:
                    case OperationCode.GreaterEqual:
                    case OperationCode.BinaryOr:
                    {
                        EvaluateBinaryOperation(CurrentOperation.Code);
                        break;
                    }

                    case OperationCode.In:
                    {
                        EvaluateIn(negate: false);
                        break;
                    }

                    case OperationCode.NotIn:
                    {
                        EvaluateIn(negate: true);
                        break;
                    }

                    #endregion

                    #region Unary Operators

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

                    case OperationCode.CoerceToStr:
                    {
                        EvaluateCoerceToStr();
                        break;
                    }

                    #endregion

                    #region Jumps

                    case OperationCode.JumpIfFalseOrPop:
                    {
                        var operand = _valStack.Peek();

                        if (LogicEvaluator.ShouldShortCircuitAnd(ref operand))
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
                        var operand = _valStack.Peek();

                        if (LogicEvaluator.ShouldShortCircuitOr(ref operand))
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
                        var operand = _valStack.Pop();

                        if (LogicEvaluator.ShouldShortCircuitAnd(ref operand))
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

                    case OperationCode.GetIterator:
                    {
                        var source = _valStack.Pop();
                        var iter = IteratorFactory.GetIterator(source);
                        _valStack.Push(new SourceValue(iter));
                        break;
                    }

                    case OperationCode.ForIterNextOrJump:
                    {
                        // Peek the iterator (kept on stack for the whole loop); push next value or jump to exhaust target.
                        var iter = (IIterator)_valStack.Peek().ToObject();

                        if (iter.TryMoveNext(out var current))
                        {
                            _valStack.Push(current);
                            break;
                        }

                        _valStack.Pop();
                        _callStack.JumpToInstr(CurrentOperation.Operand);
                        continue;
                    }

                    case OperationCode.Pop:
                    {
                        _valStack.Pop();
                        break;
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

                    case OperationCode.PopAndAssignToGlobal:
                    {
                        PopAndAssignToGlobal();
                        break;
                    }

                    case OperationCode.PushGlobalValue:
                    {
                        PushGlobalValue();
                        break;
                    }

                    case OperationCode.PopAndAssignToNonlocal:
                    {
                        PopAndAssignToNonlocal();
                        break;
                    }

                    case OperationCode.PushNonlocalValue:
                    {
                        PushNonlocalValue();
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
                        _callStack.ExitFunctionCall();

                        // Caller's IP was advanced before the call; resume the caller without auto-advancing the freshly-restored frame.
                        continue;
                    }

                    case OperationCode.PopExpressionStatementResult:
                    {
                        lastExprStmntValue = _valStack.Pop();
                        break;
                    }

                    #endregion

                    #region Subscripts

                    case OperationCode.Subscript:
                    {
                        EvaluateSubscript();
                        break;
                    }

                    case OperationCode.SubscriptSlice:
                    {
                        EvaluateSubscriptSlice();
                        break;
                    }

                    case OperationCode.SubscriptSet:
                    {
                        EvaluateSubscriptSet();
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

                _callStack.MoveToNextInstruction();
            }

            return lastExprStmntValue;
        }

        // No longer used in codebase, only here for old tests that used it
        // The ChowState already has a reference to the global scope, not the potential value that remains on the stack.
        public Objects.Scope EvaluateChunkNoValue()
        {
            Execute();
            return _globalScope;
        }

        /// <summary>Calls a function stored in a global variable with the name provided.</summary>
        /// <param name="callVarName">The name of a variable declared in the global scope. Caller
        /// is responsible for verifying the name is defined.</param>
        /// <param name="args">The arguments to pass to the function. If there are not any, this
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

            CallFunction(args != null ? args.Length : 0, out var isClosure);

            if (isClosure)
            {
                Execute();
            }

            return _valStack.Pop();
        }

        #endregion

        #region Push/Pop Methods

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

        void PushGlobalValue()
        {
            var varName = _callStack.CurrentChunk.ReadVariableName(CurrentOperation.Operand);

            if (!_callStack.IsGlobalDefined(varName))
            {
                throw new UndefinedNameException(varName, GetCurrentLineNumber());
            }

            _valStack.Push(_callStack.GetGlobal(varName));
        }

        void PopAndAssignToGlobal()
        {
            var name = _callStack.CurrentChunk.ReadVariableName(CurrentOperation.Operand);
            var assignVal = _valStack.Pop();

            _callStack.AssignToGlobal(name, assignVal);
        }

        void PushNonlocalValue()
        {
            // Semantic analysis guarantees an enclosing function binding exists; the CallStack
            // helper throws KeyNotFoundException if that invariant is violated.
            var varName = _callStack.CurrentChunk.ReadVariableName(CurrentOperation.Operand);
            _valStack.Push(_callStack.GetNonlocal(varName));
        }

        void PopAndAssignToNonlocal()
        {
            var name = _callStack.CurrentChunk.ReadVariableName(CurrentOperation.Operand);
            var assignVal = _valStack.Pop();

            _callStack.AssignToNonlocal(name, assignVal);
        }

        void PushNewInternalList(int elementCount)
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

        void PushNewInternalDict(int pairCount)
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

        void PushNewClosureFromTemplate()
        {
            // Type guarenteed to be at top of stack
            var template = (FunctionDefinition)_valStack.Pop().ToObject();

            var captured = _callStack.CurrentScope;
            var closure = new SourceFunction(template.Chunk, captured, template.Name, template.ParamCount);

            _valStack.Push(new SourceValue(closure));
        }

        #endregion

        #region Function Call Methods

        // Use an out parameter just so it's more explicit
        void CallFunction(int argCount, out bool isClosure)
        {
            var args = new SourceValue[argCount];

            for (var i = argCount - 1; i >= 0; i--)
            {
                args[i] = _valStack.Pop();
            }

            var calleeValue = _valStack.Pop();
            isClosure = IsClosure(calleeValue);

            // If the SourceValue is storing a closure inside (i.e. a function made up of bytecode)
            if (isClosure)
            {
                // Switches to the closure's frame, so Execute will next execute the first
                // instruction of the closure's chunk.
                PushClosureStackFrame(argCount, (SourceFunction)calleeValue.ToObject(), args);
            }
            else
            {
                // Will push its return value onto the stack.
                CallInteropFunction(calleeValue, args);
            }
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

            // Re-push args; function body's first ops are param-bind PopAndAssignToVariable's, popping right-to-left.
            for (var i = 0; i < argCount; i++)
            {
                _valStack.Push(args[i]);
            }

            // Advance caller's IP BEFORE pushing the frame so PushReturnValue lands at the next caller instruction.
            _callStack.MoveToNextInstruction();
            _callStack.EnterFunctionCall(sourceFunction);
        }

        #endregion

        #region Expression Evaluation Methods

        // TODO: Split each op into separate methods to avoid evaluating the operation code twice
        void EvaluateBinaryOperation(OperationCode opCode)
        {
            // Double/bool promotion happens inside SourceValue's instance operator methods (CreateSum etc.)
            var right = _valStack.Pop();
            var left = _valStack.Pop();

            switch (opCode)
            {
                case OperationCode.Add:
                {
                    _valStack.Push(ArithmeticEvaluator.EvaluateAddition(ref left, ref right));
                    break;
                }

                case OperationCode.Subtract:
                {
                    _valStack.Push(ArithmeticEvaluator.EvaluateSubtraction(ref left, ref right));
                    break;
                }

                case OperationCode.Multiply:
                {
                    _valStack.Push(ArithmeticEvaluator.EvaluateMultiplication(ref left, ref right));
                    break;
                }

                case OperationCode.Divide:
                {
                    _valStack.Push(ArithmeticEvaluator.EvaluateDivision(ref left, ref right));
                    break;
                }

                case OperationCode.Modulus:
                {
                    _valStack.Push(ArithmeticEvaluator.EvaluateModulus(ref left, ref right));
                    break;
                }

                case OperationCode.Exponentiate:
                {
                    _valStack.Push(ArithmeticEvaluator.EvaluateExponent(ref left, ref right));
                    break;
                }

                case OperationCode.FloorDivide:
                {
                    _valStack.Push(ArithmeticEvaluator.EvaluateFloorDivision(ref left, ref right));
                    break;
                }

                case OperationCode.Equal:
                {
                    _valStack.Push(ComparisonEvaluator.EvaluateEqual(ref left, ref right));
                    break;
                }

                case OperationCode.NotEqual:
                {
                    _valStack.Push(ComparisonEvaluator.EvaluateNotEqual(ref left, ref right));
                    break;
                }

                case OperationCode.Less:
                {
                    _valStack.Push(ComparisonEvaluator.EvaluateLess(ref left, ref right));
                    break;
                }

                case OperationCode.Greater:
                {
                    _valStack.Push(ComparisonEvaluator.EvaluateGreater(ref left, ref right));
                    break;
                }

                case OperationCode.LessEqual:
                {
                    _valStack.Push(ComparisonEvaluator.EvaluateLessEqual(ref left, ref right));
                    break;
                }

                case OperationCode.GreaterEqual:
                {
                    _valStack.Push(ComparisonEvaluator.EvaluateGreaterEqual(ref left, ref right));
                    break;
                }

                case OperationCode.BinaryOr:
                {
                    _valStack.Push(left.CreateUnion(right));
                    break;
                }

                default:
                {
                    throw new NotImplementedException($"Execution of {opCode} is not implemented.");
                }
            }
        }

        void EvaluateNegation()
        {
            var operand = _valStack.Pop();
            _valStack.Push(ArithmeticEvaluator.EvaluateNegation(ref operand));
        }

        void EvaluateNot()
        {
            var operand = _valStack.Pop();
            _valStack.Push(LogicEvaluator.EvaluateNot(ref operand));
        }

        void EvaluateCoerceToStr()
        {
            var operand = _valStack.Pop();
            _valStack.Push(operand.CreateStr());
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

        #region Subscript Methods

        void EvaluateSubscript()
        {
            var index = _valStack.Pop();
            var target = _valStack.Pop();

            // TODO: Add a branch here for strings.
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

        void EvaluateSubscriptSlice()
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

        void EvaluateSubscriptSet()
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

        #endregion

        #region Attributes Methods

        void GetObjectAttribute()
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

        void SetInteropObjectAttribute()
        {
            var attrName = _callStack.CurrentChunk.ReadVariableName(CurrentOperation.Operand);
            _valStack.Pop();
            var target = _valStack.Pop();

            throw new AttributeException(ParseDataTypeName(target.DataType), attrName, GetCurrentLineNumber());
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
