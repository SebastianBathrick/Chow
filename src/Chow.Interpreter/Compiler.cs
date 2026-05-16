using System;
using System.Collections.Generic;
using Chow.Interpreter.Bytecode;
using Chow.Interpreter.Exceptions;
using Chow.Interpreter.State.Values;
using Chow.Interpreter.SyntaxTrees;
using Chow.Interpreter.SyntaxTrees.Attributes;
using Chow.Interpreter.SyntaxTrees.Expressions;
using Chow.Interpreter.SyntaxTrees.Literals;
using Chow.Interpreter.SyntaxTrees.Scope;
using Chow.Interpreter.SyntaxTrees.Statements;
using Chow.Interpreter.SyntaxTrees.Subscripts;
namespace Chow.Interpreter
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
            if (!(_root is TreeRootNode treeRoot))
            {
                throw new InvalidOperationException();
            }

            foreach (var statement in treeRoot.Statements)
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

                _chunk.AddInstruction(OperationCode.PopAndAssignToVariable, param.LineNumber, paramNameIdx);
            }

            CompileTargetNode(funcNode.Body);

            // Implicit `return None` for funcs that fall off the end of the body
            var noneIdx = _chunk.RegisterConstant(ChowValue.None);

            _chunk.AddInstruction(OperationCode.PushConstant, funcNode.LineNumber, noneIdx);
            _chunk.AddInstruction(OperationCode.PushReturnValue, funcNode.LineNumber);

            return _chunk;
        }

        void CompileTargetNode(Node targetNode)
        {
            switch (targetNode)
            {
                case BlockNode blockNode:
                {
                    CompileBlockNode(blockNode);
                    break;
                }

                case LiteralNode literalNode:
                {
                    CompileLiteral(literalNode);
                    break;
                }

                case ExpressionNode exprNode:
                {
                    CompileExpression(exprNode);
                    break;
                }

                case VariableAssignStatementNode varAssignNode:
                {
                    CompileVariableAssign(varAssignNode);
                    break;
                }

                case NameNode varFactorNode:
                {
                    CompileVariableFactor(varFactorNode);
                    break;
                }

                case ReturnStatementNode returnNode:
                {
                    // If it returns early, still parse the remaining code in the chunk for debugging (subject to change)
                    CompileReturnStatement(returnNode);
                    break;
                }

                case ExpressionStatementNode exprStmtNode:
                {
                    CompileExpressionStatement(exprStmtNode);
                    break;
                }

                case IfStatementNode ifNode:
                {
                    CompileIfStatement(ifNode);
                    break;
                }

                case BranchStatementNode branchNode:
                {
                    CompileBranchStatement(branchNode);
                    break;
                }

                case WhileStatementNode whileNode:
                {
                    CompileWhileStatement(whileNode);
                    break;
                }

                case ForStatementNode forNode:
                {
                    CompileForStatement(forNode);
                    break;
                }

                case BreakStatementNode breakNode:
                {
                    CompileBreakStatement(breakNode);
                    break;
                }

                case ContinueStatementNode continueNode:
                {
                    CompileContinueStatement(continueNode);
                    break;
                }

                case FunctionNode funcNode:
                {
                    CompileFunctionDeclaration(funcNode);
                    break;
                }

                case CallNode callNode:
                {
                    CompileCall(callNode);
                    break;
                }

                case ListLiteralNode listLiteralNode:
                {
                    // TODO: Check if CompileListLiteral & CompileDictLiteral should be grouped with all the other literals
                    CompileListLiteral(listLiteralNode);
                    break;
                }

                case DictLiteralNode dictLiteralNode:
                {
                    CompileDictLiteral(dictLiteralNode);
                    break;
                }

                case SubscriptNode subscrNode:
                {
                    CompileSubscript(subscrNode);
                    break;
                }

                case AttributeAccessNode attrAccessNode:
                {
                    CompileAttributeAccess(attrAccessNode);
                    break;
                }

                case SubscriptAssignNode subscrAssignNode:
                {
                    CompileSubscriptAssign(subscrAssignNode);
                    break;
                }

                case AttributeAssignNode attrAssignNode:
                {
                    CompileAttributeAssign(attrAssignNode);
                    break;
                }

                case GlobalDeclarationNode _:
                case NonlocalDeclarationNode _:
                {
                    // Declarations are compile-time directives consumed by SemanticAnalysis;
                    // they emit no bytecode.
                    break;
                }

                default:
                {
                    throw new InvalidOperationException();
                }
            }
        }

        #endregion

        #region Statement Methods

        void CompileBlockNode(BlockNode blockNode)
        {
            // Python has no block scope: names assigned inside an `if`/`while` body
            // belong to the enclosing function or module scope.
            foreach (var statement in blockNode.Statements)
            {
                CompileTargetNode(statement);
            }
        }

        void CompileFunctionDeclaration(FunctionNode funcNode)
        {
            var funcCompiler = new Compiler(funcNode);
            var funcChunk = funcCompiler.CompileFunctionBody();

            var template = new ClosureTemplate(funcChunk, funcNode.Name, funcNode.Params.Count);
            var templateIdx = _chunk.RegisterConstant(new ChowValue(template));

            // Push template, then runtime PushNewClosureFromTemplate captures the active scope and wraps it as a Closure.
            _chunk.AddInstruction(OperationCode.PushConstant, funcNode.LineNumber, templateIdx);
            _chunk.AddInstruction(OperationCode.PushNewClosureFromTemplate, funcNode.LineNumber);

            // Functions work like variables, can be reassigned, and be passed around as values.
            // The binding is subject to global/nonlocal resolution stamped on the FunctionNode
            // by SemanticAnalysis.
            var varNameIdx = _chunk.RegisterVariableName(funcNode.Name);
            _chunk.AddInstruction(GetScopeAssignOpCode(funcNode.Resolution), funcNode.LineNumber, varNameIdx);
        }

        void CompileVariableAssign(VariableAssignStatementNode variableAssignStatementNode)
        {
            /* [NOTE]
             *
             * Variable semantics have been verified by SemanticAnalysis between parsing and
             * compilation. The Resolution stamp on this node selects which opcode is emitted:
             * Local → PopAndAssignToVariable, Global → PopAndAssignToGlobal,
             * Nonlocal → PopAndAssignToNonlocal.
             *
             * [HOW VARIABLE ASSIGNMENTS WORK]
             *
             * Assignments and declarations share syntax because the virtual machine handles them similarly due to
             * dynamic typing. Here is how the VirtualMachine runs an assignment operation:
             *
             * 1. Pop a value off the stack representing the new/initial value for the variable. The new/initial value
             *    is stored in a ChowValue and represents an expression evaluated at runtime. This can be of any type.
             *
             * 2. Use the current Operation.Operand to get the variable's name stored as a string inside Chunk during
             *    compile-time (i.e., the compilation logic code below). It's stored this way so Operations don't have
             *    to store the identifiers themselves.
             *
             * 3. Routes the assign through CallStack to the scope selected by the opcode: the current frame's scope
             *    for Local, the module scope for Global, or the nearest enclosing function scope for Nonlocal.
             */

            CompileTargetNode(variableAssignStatementNode.Expression);

            // If a variable with the same name already exists in the chunk, the index of the existing variable will be returned.
            // Otherwise, the new variable will be added to the chunk and its new index will be returned.
            var varNameIdx = _chunk.RegisterVariableName(variableAssignStatementNode.Name);
            _chunk.AddInstruction(GetScopeAssignOpCode(variableAssignStatementNode.Resolution), variableAssignStatementNode.LineNumber,
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
                var noneIdx = _chunk.RegisterConstant(ChowValue.None);
                _chunk.AddInstruction(OperationCode.PushConstant, returnStatementNode.LineNumber, noneIdx);
            }

            _chunk.AddInstruction(OperationCode.PushReturnValue, returnStatementNode.LineNumber);
        }

        void CompileExpressionStatement(ExpressionStatementNode expressionStmtNode)
        {
            CompileTargetNode(expressionStmtNode.Expression);
            _chunk.AddInstruction(OperationCode.PopExpressionStatementResult, expressionStmtNode.LineNumber);
        }

        void CompileIfStatement(IfStatementNode ifStatementNode)
        {
            // Save outer chain's pending end-jumps so nested ifs don't corrupt it
            var saved = _pendingEndJumps;

            // TODO: Check if we can avoid using a new list here and just clear the existing one
            _pendingEndJumps = new List<int>();
            CompileTargetNode(ifStatementNode.Expression);

            _chunk.AddInstruction(OperationCode.JumpIfFalse, ifStatementNode.LineNumber);
            var jumpFalseIdx = _chunk.InstructionCount - 1;

            CompileTargetNode(ifStatementNode.Block);

            // Only emit a jump-past-branches if there's actually a branch to skip
            if (ifStatementNode.Branch != null)
            {
                _chunk.AddInstruction(OperationCode.JumpPastElseBranches, ifStatementNode.LineNumber);
                _pendingEndJumps.Add(_chunk.InstructionCount - 1);
            }

            // JumpIfFalse lands at the start of the next branch (or END if no branch)
            _chunk.PatchInstructionOperand(jumpFalseIdx, _chunk.InstructionCount);

            if (ifStatementNode.Branch != null)
            {
                CompileTargetNode(ifStatementNode.Branch);
            }

            // Patch every JumpPastElseBranches in this chain to land at END (current count)
            foreach (var jumpIdx in _pendingEndJumps)
            {
                _chunk.PatchInstructionOperand(jumpIdx, _chunk.InstructionCount);
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
            _chunk.AddInstruction(OperationCode.JumpIfFalse, node.LineNumber);

            var jumpFalseIdx = _chunk.InstructionCount - 1;
            CompileTargetNode(node.Block);

            if (node.Branch != null)
            {
                _chunk.AddInstruction(OperationCode.JumpPastElseBranches, node.LineNumber);
                _pendingEndJumps.Add(_chunk.InstructionCount - 1);
            }

            _chunk.PatchInstructionOperand(jumpFalseIdx, _chunk.InstructionCount);

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

            _chunk.AddInstruction(OperationCode.JumpIfFalse, whileStatementNode.LineNumber);
            var exitJumpIdx = _chunk.InstructionCount - 1;

            var loopContext = new LoopContext
            {
                LoopStartIdx = loopStartIdx
            };

            _loopContextStack.Push(loopContext);

            CompileTargetNode(whileStatementNode.Block);

            _loopContextStack.Pop();

            _chunk.AddInstruction(OperationCode.JumpToLoopStart, whileStatementNode.LineNumber, loopStartIdx);

            // Condition-false exit and any `break` jumps land here, after the backward JumpToLoopStart.
            var exitIdx = _chunk.InstructionCount;
            _chunk.PatchInstructionOperand(exitJumpIdx, exitIdx);

            foreach (var jumpIdx in loopContext.PendingBreaks)
            {
                _chunk.PatchInstructionOperand(jumpIdx, exitIdx);
            }
        }

        void CompileForStatement(ForStatementNode forNode)
        {
            // 1. Push iterable, convert to iterator. Iterator stays on the stack for the loop's lifetime.
            CompileTargetNode(forNode.Iterable);
            _chunk.AddInstruction(OperationCode.GetIterator, forNode.LineNumber);

            // 2. Loop head: ForIterNextOrJump peeks the iterator; on success pushes the next value, on exhaustion pops + jumps.
            var loopStartIdx = _chunk.InstructionCount;
            _chunk.AddInstruction(OperationCode.ForIterNextOrJump, forNode.LineNumber);
            var exitJumpIdx = _chunk.InstructionCount - 1;

            // 3. Bind the freshly pushed value to the loop variable.
            var targetNameIdx = _chunk.RegisterVariableName(forNode.Target.Name);
            _chunk.AddInstruction(GetScopeAssignOpCode(forNode.Target.Resolution), forNode.Target.LineNumber, targetNameIdx);

            var loopContext = new LoopContext
            {
                LoopStartIdx = loopStartIdx,
                HasIteratorOnStack = true
            };

            _loopContextStack.Push(loopContext);
            CompileTargetNode(forNode.Block);
            _loopContextStack.Pop();

            // 4. The bottom of the body jumps back to ForIterNextOrJump. Iterator is still on the stack.
            _chunk.AddInstruction(OperationCode.JumpToLoopStart, forNode.LineNumber, loopStartIdx);

            // 5. Natural exhaustion lands here (ForIterNextOrJump already popped the iterator before jumping).
            //    The optional else-block runs only on natural exhaustion; `break` skips it.
            _chunk.PatchInstructionOperand(exitJumpIdx, _chunk.InstructionCount);

            if (forNode.ElseBranch != null)
            {
                CompileTargetNode(forNode.ElseBranch);
            }

            // 6. `break` jumps land here, past the else-block.
            var exitIdx = _chunk.InstructionCount;

            foreach (var jumpIdx in loopContext.PendingBreaks)
            {
                _chunk.PatchInstructionOperand(jumpIdx, exitIdx);
            }
        }

        void CompileBreakStatement(BreakStatementNode breakStatementNode)
        {
            if (_loopContextStack.Count == 0)
            {
                throw new ParserEx("'break' outside loop", breakStatementNode.LineNumber);
            }

            var loopContext = _loopContextStack.Peek();

            // For-loops keep the iterator on the stack across iterations; `break` must discard it before jumping.
            if (loopContext.HasIteratorOnStack)
            {
                _chunk.AddInstruction(OperationCode.Pop, breakStatementNode.LineNumber);
            }

            _chunk.AddInstruction(OperationCode.JumpPastElseBranches, breakStatementNode.LineNumber);
            loopContext.PendingBreaks.Add(_chunk.InstructionCount - 1);
        }

        void CompileContinueStatement(ContinueStatementNode continueStatementNode)
        {
            if (_loopContextStack.Count == 0)
            {
                // TODO: Remove ParserEx in the compiler and replace with a more appropriate exception type
                throw new ParserEx("'continue' not properly in loop", continueStatementNode.LineNumber);
            }

            var loopContext = _loopContextStack.Peek();
            _chunk.AddInstruction(OperationCode.JumpToLoopStart, continueStatementNode.LineNumber, loopContext.LoopStartIdx);
        }

        #endregion

        #region Expression Methods

        void CompileCall(CallNode callNode)
        {
            CompileTargetNode(callNode.CallName);

            foreach (var arg in callNode.Args)
            {
                CompileTargetNode(arg);
            }

            _chunk.AddInstruction(OperationCode.CallFunction, callNode.LineNumber, callNode.Args.Count);
        }

        void CompileVariableFactor(NameNode varFactorNode)
        {
            // Register to have its own constant in case the variable with this name is declared in a previous environment
            var varNameIdx = _chunk.RegisterVariableName(varFactorNode.Name);
            _chunk.AddInstruction(GetScopeReadOpCode(varFactorNode.Resolution), varFactorNode.LineNumber, varNameIdx);
        }

        void CompileExpression(ExpressionNode expressionNode)
        {
            // `and`/`or` short-circuit, so they cannot use the eager postfix layout used by all other binary operators
            if (expressionNode.Operator == ExpressionOperator.And || expressionNode.Operator == ExpressionOperator.Or)
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
            _chunk.AddInstruction(opCode, expressionNode.LineNumber);
        }

        void CompileShortCircuit(ExpressionNode expressionNode)
        {
            CompileTargetNode(expressionNode.Left);

            var jumpCode = expressionNode.Operator == ExpressionOperator.And
                ? OperationCode.JumpIfFalseOrPop
                : OperationCode.JumpIfTrueOrPop;

            // Emit jump with placeholder operand; the real target is unknown until the right side is compiled
            _chunk.AddInstruction(jumpCode, expressionNode.LineNumber);
            var patchIdx = _chunk.InstructionCount - 1;

            CompileTargetNode(expressionNode.Right);

            // Land just past the right-hand bytecode
            _chunk.PatchInstructionOperand(patchIdx, _chunk.InstructionCount);
        }

        void CompileLiteral(LiteralNode literalNode)
        {
            var constValue = BuildLiteralValue(literalNode);

            // If a constant of the same value already exists in the chunk, the operand of the existing constant will be returned.
            // Otherwise, the new constant will be added to the chunk and its new operand will be returned.
            var constIdx = _chunk.RegisterConstant(constValue);
            _chunk.AddInstruction(OperationCode.PushConstant, literalNode.LineNumber, constIdx);
        }

        static ChowValue BuildLiteralValue(LiteralNode literalNode)
        {
            // Cases for LiteralDataType where the boxed Value is the wrong CLR type should not occur unless the Parser is bugged
            switch (literalNode.Type)
            {
                case LiteralDataType.Integer:
                {
                    if (literalNode.Value is long intVal)
                    {
                        return new ChowValue(intVal);
                    }

                    break;
                }

                case LiteralDataType.Float:
                {
                    if (literalNode.Value is double floatVal)
                    {
                        return new ChowValue(floatVal);
                    }

                    break;
                }

                case LiteralDataType.Boolean:
                {
                    if (literalNode.Value is bool boolVal)
                    {
                        return new ChowValue(boolVal);
                    }

                    break;
                }

                case LiteralDataType.None:
                {
                    return ChowValue.None;
                }

                case LiteralDataType.String:
                {
                    if (literalNode.Value is string strVal)
                    {
                        return new ChowValue(strVal);
                    }

                    break;
                }

                default:
                {
                    throw new NotImplementedException($"Compilation of literal type {literalNode.Type} is not implemented.");
                }
            }

            // Reached only when the boxed Value's CLR type doesn't match its declared LiteralDataType (parser bug).
            throw new InvalidOperationException();
        }

        void CompileListLiteral(ListLiteralNode node)
        {
            foreach (var element in node.Elements)
            {
                CompileTargetNode(element);
            }

            _chunk.AddInstruction(OperationCode.PushNewInternalList, node.LineNumber, node.Elements.Count);
        }

        void CompileDictLiteral(DictLiteralNode node)
        {
            for (var i = 0; i < node.Keys.Count; i++)
            {
                CompileTargetNode(node.Keys[i]);
                CompileTargetNode(node.Values[i]);
            }

            _chunk.AddInstruction(OperationCode.PushNewInternalDict, node.LineNumber, node.Keys.Count);
        }

        void CompileSubscript(SubscriptNode node)
        {
            CompileTargetNode(node.Target);

            if (node.Index is SubscriptSliceNode sliceNode)
            {
                CompileSliceArgument(sliceNode.Start, sliceNode.LineNumber);
                CompileSliceArgument(sliceNode.Stop, sliceNode.LineNumber);
                CompileSliceArgument(sliceNode.Step, sliceNode.LineNumber);
                _chunk.AddInstruction(OperationCode.SubscriptSlice, node.LineNumber);
            }
            else
            {
                CompileTargetNode(node.Index);
                _chunk.AddInstruction(OperationCode.Subscript, node.LineNumber);
            }
        }

        void CompileSliceArgument(Node argOrNull, int sliceLineNum)
        {
            if (argOrNull == null)
            {
                var noneIdx = _chunk.RegisterConstant(ChowValue.None);
                _chunk.AddInstruction(OperationCode.PushConstant, sliceLineNum, noneIdx);
            }
            else
            {
                CompileTargetNode(argOrNull);
            }
        }

        void CompileAttributeAccess(AttributeAccessNode node)
        {
            CompileTargetNode(node.Target);

            var varNameIdx = _chunk.RegisterVariableName(node.AttributeName);
            _chunk.AddInstruction(OperationCode.GetObjectAttribute, node.LineNumber, varNameIdx);
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
            _chunk.AddInstruction(OperationCode.SubscriptSet, node.LineNumber);
        }

        void CompileAttributeAssign(AttributeAssignNode node)
        {
            CompileTargetNode(node.Target);
            CompileTargetNode(node.Expression);

            var varNameIdx = _chunk.RegisterVariableName(node.AttributeName);
            _chunk.AddInstruction(OperationCode.SetInteropObjectAttribute, node.LineNumber, varNameIdx);
        }

        #endregion

        #region Helper Methods

        static OperationCode GetScopeAssignOpCode(ScopeType resolution)
        {
            switch (resolution)
            {
                case ScopeType.Global:
                {
                    return OperationCode.PopAndAssignToGlobal;
                }
                case ScopeType.Nonlocal:
                {
                    return OperationCode.PopAndAssignToNonlocal;
                }
                default:
                {
                    return OperationCode.PopAndAssignToVariable;
                }
            }
        }

        static OperationCode GetScopeReadOpCode(ScopeType resolution)
        {
            switch (resolution)
            {
                case ScopeType.Global:
                {
                    return OperationCode.PushGlobalValue;
                }
                case ScopeType.Nonlocal:
                {
                    return OperationCode.PushNonlocalValue;
                }
                default:
                {
                    return OperationCode.PushVariableValue;
                }
            }
        }

        static OperationCode GetExpressionOpCode(ExpressionNode expressionNode)
        {
            switch (expressionNode.Operator)
            {
                case ExpressionOperator.Add:
                {
                    return OperationCode.Add;
                }

                case ExpressionOperator.Subtract:
                {
                    return OperationCode.Subtract;
                }

                case ExpressionOperator.Multiply:
                {
                    return OperationCode.Multiply;
                }

                case ExpressionOperator.Divide:
                {
                    return OperationCode.Divide;
                }

                case ExpressionOperator.Modulus:
                {
                    return OperationCode.Modulus;
                }

                case ExpressionOperator.Exponentiate:
                {
                    return OperationCode.Exponentiate;
                }

                case ExpressionOperator.FloorDivide:
                {
                    return OperationCode.FloorDivide;
                }

                case ExpressionOperator.Negate:
                {
                    return OperationCode.Negate;
                }

                case ExpressionOperator.Equal:
                {
                    return OperationCode.Equal;
                }

                case ExpressionOperator.NotEqual:
                {
                    return OperationCode.NotEqual;
                }

                case ExpressionOperator.Less:
                {
                    return OperationCode.Less;
                }

                case ExpressionOperator.Greater:
                {
                    return OperationCode.Greater;
                }

                case ExpressionOperator.LessEqual:
                {
                    return OperationCode.LessEqual;
                }

                case ExpressionOperator.GreaterEqual:
                {
                    return OperationCode.GreaterEqual;
                }

                case ExpressionOperator.Not:
                {
                    return OperationCode.Not;
                }

                case ExpressionOperator.BinaryOr:
                {
                    return OperationCode.BinaryOr;
                }

                case ExpressionOperator.In:
                {
                    return OperationCode.In;
                }

                case ExpressionOperator.NotIn:
                {
                    return OperationCode.NotIn;
                }

                default:
                {
                    throw new NotImplementedException(nameof(expressionNode.Operator));
                }
            }
        }

        #endregion

    }
}
