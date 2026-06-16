using System;
using System.Collections.Generic;
using Chow.SourceData;
using Chow.Syntax;
using Chow.Utility;

namespace Chow.Bytecode.Compilation
{
    sealed class Compiler
    {
        readonly Chunk _chunk;
        readonly Stack<LoopContext> _loopContextStack;
        readonly Node _root;

        List<int> _pendingEndJumps;

        sealed class LoopContext
        {
            public readonly List<int> PendingBreaks = new List<int>();
            // True for `for` loops; the iterator sits on the stack across iterations, so `break` must Pop before jumping.
            public bool HasIteratorOnStack;
            public int LoopStartIdx;
        }

        #region Primary Methods

        public Compiler(Node root)
        {
            _chunk = new Chunk();
            _root = root;
            _pendingEndJumps = new List<int>();
            _loopContextStack = new Stack<LoopContext>();
        }

        public Chunk CompileRoot()
        {
            if (!(_root is ModuleNode treeRoot))
            {
                throw new InvalidOperationException();
            }

            foreach (var statement in ((BlockNode)treeRoot.Block).Statements)
            {
                CompileTargetNode(statement);
            }

            return _chunk;
        }

        Chunk CompileFunctionBody()
        {
            if (!(_root is FunctionNode funcNode))
            {
                throw new InvalidOperationException();
            }

            // Caller pushes args left-to-right; bind in reverse so positional order matches when popping
            for (var i = funcNode.Params.Count - 1; i >= 0; i--)
            {
                var param = (NameNode)funcNode.Params[i];
                var paramNameIdx = _chunk.RegisterVariableName(param.Name);

                _chunk.Add(OperationCode.AssignLocal, param.LineNumber, paramNameIdx);
            }

            CompileTargetNode(funcNode.Block);

            // Implicit `return None` for funcs that fall off the end of the body
            var noneIdx = _chunk.RegisterConstant(SourceValue.None);

            _chunk.Add(OperationCode.PushConstantValue, funcNode.LineNumber, noneIdx);
            _chunk.Add(OperationCode.PushReturnValue, funcNode.LineNumber);

            return _chunk;
        }

        void CompileTargetNode(Node targetNode)
        {
            switch (targetNode)
            {
                case BlockNode blockNode:
                    CompileBlockNode(blockNode);
                    break;
                case LiteralNode literalNode:
                    CompileLiteral(literalNode);
                    break;
                case FStringNode fStringNode:
                    CompileFString(fStringNode);
                    break;
                case ExpressionNode exprNode:
                    CompileExpression(exprNode);
                    break;
                case AssignStatementNode varAssignNode:
                    CompileVariableAssign(varAssignNode);
                    break;
                case NameNode varFactorNode:
                    CompileVariableFactor(varFactorNode);
                    break;
                case ReturnStatementNode returnNode:
                    // If it returns early, still parse the remaining code in the chunk for debugging (subject to change)
                    CompileReturnStatement(returnNode);
                    break;
                case ExpressionStatementNode exprStmtNode:
                    CompileExpressionStatement(exprStmtNode);
                    break;
                case IfStatementNode ifNode:
                    CompileIfStatement(ifNode);
                    break;
                case BranchStatementNode branchNode:
                    CompileBranchStatement(branchNode);
                    break;
                case WhileStatementNode whileNode:
                    CompileWhileStatement(whileNode);
                    break;
                case ForStatementNode forNode:
                    CompileForStatement(forNode);
                    break;
                case BreakStatementNode breakNode:
                    CompileBreakStatement(breakNode);
                    break;
                case ContinueStatementNode continueNode:
                    CompileContinueStatement(continueNode);
                    break;
                case FunctionNode funcNode:
                    CompileFunctionDeclaration(funcNode);
                    break;
                case CallNode callNode:
                    CompileCall(callNode);
                    break;
                case ListNode listLiteralNode:
                    // TODO: Check if CompileListLiteral & CompileDictLiteral should be grouped with all the other literals
                    CompileListLiteral(listLiteralNode);
                    break;
                case DictionaryNode dictLiteralNode:
                    CompileDictLiteral(dictLiteralNode);
                    break;
                case SubscriptNode subscriptNode:
                    CompileSubscript(subscriptNode);
                    break;
                case SubscriptAssignNode subscriptAccessNode:
                    CompileSubscriptAssign(subscriptAccessNode);
                    break;
                case AttributeAccessNode attributeAccessNode:
                    CompileAttributeAccess(attributeAccessNode);
                    break;
                case AttributeAssignNode attrAssignNode:
                    CompileAttributeAssign(attrAssignNode);
                    break;
                case GlobalNode _:
                case NonLocalNode _:
                    // Declarations are compile-time directives consumed by SemanticAnalysis;
                    // they emit no bytecode.
                    break;
                default:
                    throw new UnreachableException(nameof(CompileTargetNode));
            }
        }

        #endregion

        #region Statement Methods

        void CompileBlockNode(BlockNode blockNode)
        {
            // Python has no block scope: names assigned inside an `if`/`while` body
            // belong to the enclosing function or module scope.
            for (var i = 0; i < blockNode.Statements.Count; i++)
            {
                var statement = blockNode.Statements[i];
                CompileTargetNode(statement);
            }
        }

        void CompileFunctionDeclaration(FunctionNode funcNode)
        {
            var funcCompiler = new Compiler(funcNode);
            var funcChunk = funcCompiler.CompileFunctionBody();

            var def = new FunctionDefinition(funcChunk, funcNode.Name, funcNode.Params.Count);
            var defConstIdx = _chunk.RegisterConstant(new SourceValue(def));

            // Push the funciton definition to act as a blueprint for a function object.
            _chunk.Add(OperationCode.PushConstantValue, funcNode.LineNumber, defConstIdx);
            
            // Use the function definition to create a first-class function object.
            _chunk.Add(OperationCode.PushNewSourceFunction, funcNode.LineNumber);
            
            // Create a variable with the function's name and initialize it to the function object.
            var varNameIdx = _chunk.RegisterVariableName(funcNode.Name);
            
            // The global/nonlocal resolution is determined during semantic analysis.
            _chunk.Add(GetScopeAssignOpCode(funcNode.Resolution), funcNode.LineNumber, varNameIdx);
        }

        void CompileVariableAssign(AssignStatementNode assignStatementNode)
        {
            /* [NOTE]
             *
             * Variable semantics have been verified by SemanticAnalysis between parsing and
             * compilation. The Resolution stamp on this node selects which opcode is emitted:
             * Local → AssignLocal, Global → AssignGlobal,
             * NonLocal → AssignNonLocal.
             *
             * [HOW VARIABLE ASSIGNMENTS WORK]
             *
             * Assignments and declarations share syntax because the virtual machine handles them similarly due to
             * dynamic typing. Here is how the Processor runs an assignment operation:
             *
             * 1. Pop a value off the stack representing the new/initial value for the variable. The new/initial value
             *    is stored in a SourceValue and represents an expression evaluated at runtime. This can be of any type.
             *
             * 2. Use the current Operation.Operand to get the variable's name stored as a string inside Chunk during
             *    compile-time (i.e., the compilation logic code below). It's stored this way so Operations don't have
             *    to store the identifiers themselves.
             *
             * 3. Routes the assign through CallStack to the scope selected by the opcode: the current frame's scope
             *    for Local, the module scope for Global, or the nearest enclosing function scope for NonLocal.
             */

            CompileTargetNode(assignStatementNode.Expression);

            // If a variable with the same name already exists in the chunk, the index of the existing variable will be returned.
            // Otherwise, the new variable will be added to the chunk and its new index will be returned.
            var varNameIdx = _chunk.RegisterVariableName(assignStatementNode.Name);
            _chunk.Add(GetScopeAssignOpCode(assignStatementNode.Resolution), assignStatementNode.LineNumber,
                varNameIdx);
        }

        void CompileReturnStatement(ReturnStatementNode returnStatementNode)
        {
            if (returnStatementNode.Expression != null)
            {
                CompileTargetNode(returnStatementNode.Expression);
            }
            else
            {
                // Bare `return` returns None; PushReturnValue always pops exactly one value off the stack.
                var noneIdx = _chunk.RegisterConstant(SourceValue.None);
                _chunk.Add(OperationCode.PushConstantValue, returnStatementNode.LineNumber, noneIdx);
            }

            _chunk.Add(OperationCode.PushReturnValue, returnStatementNode.LineNumber);
        }

        void CompileExpressionStatement(ExpressionStatementNode expressionStmtNode)
        {
            CompileTargetNode(expressionStmtNode.Expression);
            _chunk.Add(OperationCode.PopExpressionStatementResult, expressionStmtNode.LineNumber);
        }

        void CompileIfStatement(IfStatementNode ifStatementNode)
        {
            // Save outer chain's pending end-jumps so nested ifs don't corrupt it
            var saved = _pendingEndJumps;

            // TODO: Check if we can avoid using a new list here and just clear the existing one
            _pendingEndJumps = new List<int>();
            CompileTargetNode(ifStatementNode.Expression);

            _chunk.Add(OperationCode.JumpIfFalse, ifStatementNode.LineNumber);
            var jumpFalseIdx = _chunk.InstructionCount - 1;

            CompileTargetNode(ifStatementNode.Block);

            // Only emit a jump-past-branches if there's actually a branch to skip
            if (ifStatementNode.Branch != null)
            {
                _chunk.Add(OperationCode.JumpPastElseBranches, ifStatementNode.LineNumber);
                _pendingEndJumps.Add(_chunk.InstructionCount - 1);
            }

            // JumpIfFalse lands at the start of the next branch (or END if no branch)
            _chunk.PatchOperand(jumpFalseIdx, _chunk.InstructionCount);

            if (ifStatementNode.Branch != null)
            {
                CompileTargetNode(ifStatementNode.Branch);
            }

            // Patch every JumpPastElseBranches in this chain to land at END (current count)
            foreach (var jumpIdx in _pendingEndJumps)
            {
                _chunk.PatchOperand(jumpIdx, _chunk.InstructionCount);
            }

            _pendingEndJumps = saved;
        }

        void CompileBranchStatement(BranchStatementNode node)
        {
            if (node.IsElse)
            {
                CompileTargetNode(node.Block);
                return;
            }

            CompileTargetNode(node.Expression);
            _chunk.Add(OperationCode.JumpIfFalse, node.LineNumber);

            var jumpFalseIdx = _chunk.InstructionCount - 1;
            CompileTargetNode(node.Block);

            if (node.Branch != null)
            {
                _chunk.Add(OperationCode.JumpPastElseBranches, node.LineNumber);
                _pendingEndJumps.Add(_chunk.InstructionCount - 1);
            }

            _chunk.PatchOperand(jumpFalseIdx, _chunk.InstructionCount);

            if (node.Branch != null)
            {
                CompileTargetNode(node.Branch);
            }
        }

        void CompileWhileStatement(WhileStatementNode whileStatementNode)
        {
            // loopStart marks the start of the condition; both `continue` and the bottom-of-body JumpToLoopStart op target it.
            var loopStartIdx = _chunk.InstructionCount;

            CompileTargetNode(whileStatementNode.Expression);

            _chunk.Add(OperationCode.JumpIfFalse, whileStatementNode.LineNumber);
            var exitJumpIdx = _chunk.InstructionCount - 1;

            var loopContext = new LoopContext
            {
                LoopStartIdx = loopStartIdx
            };

            _loopContextStack.Push(loopContext);

            CompileTargetNode(whileStatementNode.Block);

            _loopContextStack.Pop();

            _chunk.Add(OperationCode.JumpToLoopStart, whileStatementNode.LineNumber, loopStartIdx);

            // Condition-false exit and any `break` jumps land here, after the backward JumpToLoopStart.
            var exitIdx = _chunk.InstructionCount;
            _chunk.PatchOperand(exitJumpIdx, exitIdx);

            foreach (var jumpIdx in loopContext.PendingBreaks)
            {
                _chunk.PatchOperand(jumpIdx, exitIdx);
            }
        }

        void CompileForStatement(ForStatementNode forNode)
        {
            // 1. Push iterable, convert to iterator. Iterator stays on the stack for the loop's lifetime.
            CompileTargetNode(forNode.Iterable);
            _chunk.Add(OperationCode.PushNewIteratorWithValue, forNode.LineNumber);

            // 2. Loop head: JumpOrForIteratorNext peeks the iterator; on success pushes the next value, on exhaustion pops + jumps.
            var loopStartIdx = _chunk.InstructionCount;
            _chunk.Add(OperationCode.JumpOrForIteratorNext, forNode.LineNumber);
            var exitJumpIdx = _chunk.InstructionCount - 1;

            // 3. Bind the freshly pushed value to the loop variable.
            var targetNameIdx = _chunk.RegisterVariableName(forNode.Target.Name);
            _chunk.Add(GetScopeAssignOpCode(forNode.Target.Resolution), forNode.Target.LineNumber, targetNameIdx);

            var loopContext = new LoopContext
            {
                LoopStartIdx = loopStartIdx,
                HasIteratorOnStack = true
            };

            _loopContextStack.Push(loopContext);
            CompileTargetNode(forNode.Block);
            _loopContextStack.Pop();

            // 4. The bottom of the body jumps back to JumpOrForIteratorNext. Iterator is still on the stack.
            _chunk.Add(OperationCode.JumpToLoopStart, forNode.LineNumber, loopStartIdx);

            // 5. Natural exhaustion lands here (JumpOrForIteratorNext already popped the iterator before jumping).
            //    The optional else-block runs only on natural exhaustion; `break` skips it.
            _chunk.PatchOperand(exitJumpIdx, _chunk.InstructionCount);

            if (forNode.ElseBranch != null)
            {
                CompileTargetNode(forNode.ElseBranch);
            }

            // 6. `break` jumps land here, past the else-block.
            var exitIdx = _chunk.InstructionCount;

            foreach (var jumpIdx in loopContext.PendingBreaks)
            {
                _chunk.PatchOperand(jumpIdx, exitIdx);
            }
        }

        void CompileBreakStatement(BreakStatementNode breakStatementNode)
        {
            if (_loopContextStack.Count == 0)
            {
                throw new SyntaxException("'break' outside loop", breakStatementNode.LineNumber);
            }

            var loopContext = _loopContextStack.Peek();

            // For-loops keep the iterator on the stack across iterations; `break` must discard it before jumping.
            if (loopContext.HasIteratorOnStack)
            {
                _chunk.Add(OperationCode.Pop, breakStatementNode.LineNumber);
            }

            _chunk.Add(OperationCode.JumpPastElseBranches, breakStatementNode.LineNumber);
            loopContext.PendingBreaks.Add(_chunk.InstructionCount - 1);
        }

        void CompileContinueStatement(ContinueStatementNode continueStatementNode)
        {
            if (_loopContextStack.Count == 0)
            {
                // TODO: Remove SyntaxException in the compiler and replace with a more appropriate exception type
                throw new SyntaxException("'continue' not properly in loop", continueStatementNode.LineNumber);
            }

            var loopContext = _loopContextStack.Peek();
            _chunk.Add(OperationCode.JumpToLoopStart, continueStatementNode.LineNumber, loopContext.LoopStartIdx);
        }

        #endregion

        #region Expression Methods

        void CompileCall(CallNode callNode)
        {
            CompileTargetNode(callNode.FunctionName);

            foreach (var arg in callNode.Args)
            {
                CompileTargetNode(arg);
            }

            _chunk.Add(OperationCode.CallFunction, callNode.LineNumber, callNode.Args.Count);
        }

        void CompileVariableFactor(NameNode varFactorNode)
        {
            // Register to have its own constant in case the variable with this name is declared in a previous environment
            var varNameIdx = _chunk.RegisterVariableName(varFactorNode.Name);
            _chunk.Add(GetScopeReadOpCode(varFactorNode.Resolution), varFactorNode.LineNumber, varNameIdx);
        }

        void CompileExpression(ExpressionNode expressionNode)
        {
            // `and`/`or` short-circuit, so they cannot use the eager postfix layout used by all other binary operators
            if (expressionNode.Operator == Operator.And || expressionNode.Operator == Operator.Or)
            {
                CompileShortCircuit(expressionNode);
                return;
            }

            // Compile operands first so they are pushed onto the runtime stack before the operation consumes them
            CompileTargetNode(expressionNode.Left);

            if (expressionNode.Right != null)
            {
                CompileTargetNode(expressionNode.Right);
            }

            var opCode = GetExpressionOpCode(expressionNode);
            _chunk.Add(opCode, expressionNode.LineNumber);
        }

        void CompileShortCircuit(ExpressionNode expressionNode)
        {
            CompileTargetNode(expressionNode.Left);

            var jumpCode = expressionNode.Operator == Operator.And
                ? OperationCode.JumpIfFalseOrPop
                : OperationCode.JumpIfTrueOrPop;

            // Emit jump with placeholder operand; the real target is unknown until the right side is compiled
            _chunk.Add(jumpCode, expressionNode.LineNumber);
            var patchIdx = _chunk.InstructionCount - 1;

            CompileTargetNode(expressionNode.Right);

            // Land just past the right-hand bytecode
            _chunk.PatchOperand(patchIdx, _chunk.InstructionCount);
        }

        void CompileLiteral(LiteralNode literalNode)
        {
            var constValue = BuildLiteralValue(literalNode);

            // If a constant of the same value already exists in the chunk, the operand of the existing constant will be returned.
            // Otherwise, the new constant will be added to the chunk and its new operand will be returned.
            var constIdx = _chunk.RegisterConstant(constValue);
            _chunk.Add(OperationCode.PushConstantValue, literalNode.LineNumber, constIdx);
        }

        void CompileFString(FStringNode node)
        {
            var firstPartIdx = _chunk.RegisterConstant(new SourceValue(node.StringParts[0]));
            _chunk.Add(OperationCode.PushConstantValue, node.LineNumber, firstPartIdx);

            for (var i = 0; i < node.ExpressionParts.Count; i++)
            {
                CompileTargetNode(node.ExpressionParts[i]);
                _chunk.Add(OperationCode.CoerceToStr, node.LineNumber);
                _chunk.Add(OperationCode.BinaryAdd, node.LineNumber);

                var tailPartIdx = _chunk.RegisterConstant(new SourceValue(node.StringParts[i + 1]));
                _chunk.Add(OperationCode.PushConstantValue, node.LineNumber, tailPartIdx);
                _chunk.Add(OperationCode.BinaryAdd, node.LineNumber);
            }
        }

        static SourceValue BuildLiteralValue(LiteralNode literalNode)
        {
            // Cases for LiteralNodeType where the boxed Value is the wrong CLR type should not occur unless the Parser is bugged
            switch (literalNode.Type)
            {
                case LiteralNodeType.Integer:
                    if (literalNode.Value is long intVal)
                    {
                        return new SourceValue(intVal);
                    }

                    break;

                case LiteralNodeType.Float:
                    if (literalNode.Value is double floatVal)
                    {
                        return new SourceValue(floatVal);
                    }

                    break;

                case LiteralNodeType.Boolean:
                    if (literalNode.Value is bool boolVal)
                    {
                        return new SourceValue(boolVal);
                    }

                    break;
                case LiteralNodeType.None:
                    return SourceValue.None;
                case LiteralNodeType.String:
                    if (literalNode.Value is string strVal)
                    {
                        return new SourceValue(strVal);
                    }

                    break;
                default:
                    throw new NotImplementedException(
                        $"Compilation of literal type {literalNode.Type} is not implemented.");
            }

            // Reached only when the boxed Value's CLR type doesn't match its declared LiteralNodeType (parser bug).
            throw new InvalidOperationException();
        }

        void CompileListLiteral(ListNode node)
        {
            foreach (var element in node.Elements)
            {
                CompileTargetNode(element);
            }

            _chunk.Add(OperationCode.PushNewSourceList, node.LineNumber, node.Elements.Count);
        }

        void CompileDictLiteral(DictionaryNode node)
        {
            for (var i = 0; i < node.Keys.Count; i++)
            {
                CompileTargetNode(node.Keys[i]);
                CompileTargetNode(node.Values[i]);
            }

            _chunk.Add(OperationCode.PushNewSourceDictionary, node.LineNumber, node.Keys.Count);
        }

        void CompileSubscript(SubscriptNode node)
        {
            CompileTargetNode(node.Target);

            if (node.Index is SubscriptSliceNode sliceNode)
            {
                CompileSliceArgument(sliceNode.Start, sliceNode.LineNumber);
                CompileSliceArgument(sliceNode.Stop, sliceNode.LineNumber);
                CompileSliceArgument(sliceNode.Step, sliceNode.LineNumber);
                _chunk.Add(OperationCode.PushSubscriptSliceValue, node.LineNumber);
            }
            else
            {
                CompileTargetNode(node.Index);
                _chunk.Add(OperationCode.PushSubscriptValue, node.LineNumber);
            }
        }

        void CompileSliceArgument(Node argOrNull, int sliceLineNum)
        {
            if (argOrNull == null)
            {
                var noneIdx = _chunk.RegisterConstant(SourceValue.None);
                _chunk.Add(OperationCode.PushConstantValue, sliceLineNum, noneIdx);
            }
            else
            {
                CompileTargetNode(argOrNull);
            }
        }

        void CompileAttributeAccess(AttributeAccessNode node)
        {
            CompileTargetNode(node.Target);

            var varNameIdx = _chunk.RegisterVariableName(node.Name);
            _chunk.Add(OperationCode.PushAttributeValue, node.LineNumber, varNameIdx);
        }

        void CompileSubscriptAssign(SubscriptAssignNode node)
        {
            if (node.Index is SubscriptSliceNode)
            {
                throw new NotImplementedException("slice assignment is not implemented");
            }

            CompileTargetNode(node.Target);
            CompileTargetNode(node.Index);
            CompileTargetNode(node.Expression);
            
            _chunk.Add(OperationCode.AssignSubscript, node.LineNumber);
        }

        void CompileAttributeAssign(AttributeAssignNode node)
        {
            CompileTargetNode(node.Target);
            CompileTargetNode(node.Expression);

            var varNameIdx = _chunk.RegisterVariableName(node.AttributeName);
            _chunk.Add(OperationCode.AssignAttribute, node.LineNumber, varNameIdx);
        }

        #endregion

        #region Helper Methods

        static OperationCode GetScopeAssignOpCode(ScopeType resolution)
        {
            if (resolution == ScopeType.Global)
            {
                return OperationCode.AssignGlobal;
            }

            if (resolution == ScopeType.NonLocal)
            {
                return OperationCode.AssignNonLocal;
            }

            return OperationCode.AssignLocal;
        }

        static OperationCode GetScopeReadOpCode(ScopeType resolution)
        {
            if (resolution == ScopeType.Global)
            {
                return OperationCode.PushGlobalValue;
            }

            if (resolution == ScopeType.NonLocal)
            {
                return OperationCode.PushNonLocalValue;
            }

            return OperationCode.PushVariableValue;
        }

        static OperationCode GetExpressionOpCode(ExpressionNode expressionNode)
        {
            switch (expressionNode.Operator)
            {
                case Operator.Add:
                    return OperationCode.BinaryAdd;

                case Operator.Subtract:
                    return OperationCode.BinarySubtract;

                case Operator.Multiply:
                    return OperationCode.BinaryMultiply;

                case Operator.Divide:
                    return OperationCode.BinaryDivide;

                case Operator.Modulus:
                    return OperationCode.BinaryModulus;

                case Operator.Exponentiate:
                    return OperationCode.BinaryPow;

                case Operator.FloorDivide:
                    return OperationCode.BinaryFloor;

                case Operator.Negate:
                    return OperationCode.UnaryNegate;

                case Operator.Equal:
                    return OperationCode.BinaryEqual;

                case Operator.NotEqual:
                    return OperationCode.BinaryNotEqual;

                case Operator.Less:
                    return OperationCode.BinaryLess;

                case Operator.Greater:
                    return OperationCode.BinaryGreater;

                case Operator.LessEqual:
                    return OperationCode.BinaryLessEqual;

                case Operator.GreaterEqual:
                    return OperationCode.BinaryGreaterEqual;

                case Operator.Not:
                    return OperationCode.UnaryNot;

                case Operator.BinaryOr:
                    return OperationCode.BinaryUnion;

                case Operator.In:
                    return OperationCode.BinaryIn;

                case Operator.NotIn:
                    return OperationCode.BinaryNotIn;

                case Operator.And:
                case Operator.Or:
                case Operator.ToStr:
                default:
                    throw new NotImplementedException(nameof(expressionNode.Operator));
            }
        }

        #endregion

    }
}
