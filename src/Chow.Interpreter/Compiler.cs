using Chow.Interpreter.Bytecode;
using Chow.Interpreter.Exceptions;
using Chow.Interpreter.State.Values;
using System;
using System.Collections.Generic;
using Chow.Interpreter.SyntaxTrees;
using Chow.Interpreter.SyntaxTrees.Expressions;
using Chow.Interpreter.SyntaxTrees.Statements;

namespace Chow.Interpreter
{
    class Compiler
    {
        readonly Chunk _chunk;
        readonly Node _root;
        readonly Stack<LoopContext> _loopCtxStack;
        
        List<int> _pendingEndJumps;
        int _blockDepth;

        public Compiler(Node root)
        {
            _chunk = new Chunk();
            _root = root;
            _pendingEndJumps = new List<int>();
            _loopCtxStack = new Stack<LoopContext>();
            _blockDepth = 0;
        }

        public Chunk CompileRoot()
        {
            var treeRoot = _root as TreeRootNode;

            if (treeRoot == null)
            {
                throw new InvalidOperationException();
            }

            foreach (var statement in treeRoot.Statements)
            {
                CompileTargetNode(statement);
            }

            return _chunk;
        }

        Chunk CompileFuncBody()
        {
            var funcNode = _root as FunctionNode;

            if (funcNode == null)
            {
                throw new InvalidOperationException();
            }
            
            // Caller pushes args left-to-right; bind in reverse so positional order matches when popping
            for (var i = funcNode.Params.Count - 1; i >= 0; i--)
            {
                var param = (NameNode)funcNode.Params[i];
                var paramOperand = _chunk.RegisterVariableName(param.Name);

                _chunk.AddInstruction(OperationCode.AssignOrDeclareVariable, param.LineNumber, paramOperand);
            }

            CompileTargetNode(funcNode.Body);

            // Implicit `return None` for funcs that fall off the end of the body
            var noneIdx = _chunk.RegisterConstant(TaggedUnion.None);

            _chunk.AddInstruction(OperationCode.PushConstant, funcNode.LineNumber, noneIdx);
            _chunk.AddInstruction(OperationCode.ReturnValue, funcNode.LineNumber);

            return _chunk;
        }

        void CompileFuncDeclaration(FunctionNode funcNode)
        {
            var funcCompiler = new Compiler(funcNode);
            var funcChunk = funcCompiler.CompileFuncBody();

            var template = new ClosureTemplate(funcChunk, funcNode.Name, funcNode.Params.Count);
            var templateIdx = _chunk.RegisterConstant(new TaggedUnion(template));

            // Push template, then runtime MakeClosure captures the active scope and wraps it as a Closure.
            _chunk.AddInstruction(OperationCode.PushConstant, funcNode.LineNumber, templateIdx);
            _chunk.AddInstruction(OperationCode.MakeClosure, funcNode.LineNumber);

            var nameIdx = _chunk.RegisterVariableName(funcNode.Name);
            _chunk.AddInstruction(OperationCode.AssignOrDeclareVariable, funcNode.LineNumber, nameIdx);
        }

        void CompileCall(CallNode callNode)
        {
            CompileTargetNode(callNode.CallName);

            foreach (var arg in callNode.Args)
            {
                CompileTargetNode(arg);
            }

            _chunk.AddInstruction(OperationCode.Call, callNode.LineNumber, callNode.Args.Count);
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

                case ExprNode expressionNode:
                    CompileExpression(expressionNode);
                    break;

                case VarAssignNode varAssignNode:
                    CompileVarAssign(varAssignNode);
                    break;

                case NameNode varFactorNode:
                    CompileVarFactor(varFactorNode);
                    break;

                case ReturnNode returnNode:
                    // If it returns early, still parse the remaining code in the chunk for debugging (subject to change)
                    CompileReturn(returnNode);
                    break;

                case ExprStatementNode exprStmtNode:
                    CompileExprStmnt(exprStmtNode);
                    break;

                case IfNode ifNode:
                    CompileIfStmnt(ifNode);
                    break;

                case BranchStmntNode branchNode:
                    CompileBranchStmnt(branchNode);
                    break;

                case WhileNode whileNode:
                    CompileWhileStmnt(whileNode);
                    break;

                case BreakNode breakNode:
                    CompileBreakStmnt(breakNode);
                    break;

                case ContinueNode continueNode:
                    CompileContinueStmnt(continueNode);
                    break;

                case FunctionNode funcNode:
                    CompileFuncDeclaration(funcNode);
                    break;

                case CallNode callNode:
                    CompileCall(callNode);
                    break;

                case ListLiteralNode listLiteralNode:
                    CompileListLiteral(listLiteralNode);
                    break;

                case DictLiteralNode dictLiteralNode:
                    CompileDictLiteral(dictLiteralNode);
                    break;

                case SubscriptNode subscriptNode:
                    CompileSubscript(subscriptNode);
                    break;

                case AttrAccessNode attrAccessNode:
                    CompileAttrAccess(attrAccessNode);
                    break;

                case SubscriptAssignNode subscriptAssignNode:
                    CompileSubscriptAssign(subscriptAssignNode);
                    break;

                case AttrAssignNode attrAssignNode:
                    CompileAttrAssign(attrAssignNode);
                    break;

                default:
                    throw new InvalidOperationException();
            }
        }

        void CompileListLiteral(ListLiteralNode node)
        {
            foreach (var element in node.Elements)
            {
                CompileTargetNode(element);
            }
            _chunk.AddInstruction(OperationCode.BuildList, node.LineNumber, node.Elements.Count);
        }

        void CompileDictLiteral(DictLiteralNode node)
        {
            for (var i = 0; i < node.Keys.Count; i++)
            {
                CompileTargetNode(node.Keys[i]);
                CompileTargetNode(node.Values[i]);
            }
            _chunk.AddInstruction(OperationCode.BuildDict, node.LineNumber, node.Keys.Count);
        }

        void CompileSubscript(SubscriptNode node)
        {
            CompileTargetNode(node.Target);

            if (node.Index is SliceNode sliceNode)
            {
                CompileSliceArg(sliceNode.Start, sliceNode.LineNumber);
                CompileSliceArg(sliceNode.Stop, sliceNode.LineNumber);
                CompileSliceArg(sliceNode.Step, sliceNode.LineNumber);
                _chunk.AddInstruction(OperationCode.SubscriptSlice, node.LineNumber);
            }
            else
            {
                CompileTargetNode(node.Index);
                _chunk.AddInstruction(OperationCode.Subscript, node.LineNumber);
            }
        }

        void CompileSliceArg(Node argOrNull, int sliceLineNum)
        {
            if (argOrNull == null)
            {
                var noneIdx = _chunk.RegisterConstant(TaggedUnion.None);
                _chunk.AddInstruction(OperationCode.PushConstant, sliceLineNum, noneIdx);
            }
            else
            {
                CompileTargetNode(argOrNull);
            }
        }

        void CompileAttrAccess(AttrAccessNode node)
        {
            CompileTargetNode(node.Target);
            var nameOperand = _chunk.RegisterVariableName(node.AttrName);
            _chunk.AddInstruction(OperationCode.GetAttr, node.LineNumber, nameOperand);
        }

        void CompileSubscriptAssign(SubscriptAssignNode node)
        {
            if (node.Index is SliceNode)
            {
                throw new NotImplementedException("slice assignment is not implemented");
            }

            CompileTargetNode(node.Target);
            CompileTargetNode(node.Index);
            CompileTargetNode(node.Expression);
            _chunk.AddInstruction(OperationCode.SubscriptSet, node.LineNumber);
        }

        void CompileAttrAssign(AttrAssignNode node)
        {
            CompileTargetNode(node.Target);
            CompileTargetNode(node.Expression);

            var nameOperand = _chunk.RegisterVariableName(node.AttrName);
            _chunk.AddInstruction(OperationCode.SetAttr, node.LineNumber, nameOperand);
        }

        void CompileBlockNode(BlockNode blockNode)
        {
            // Indicate that the scope's depth increased and all variables that follow are nested in this block
            _chunk.AddInstruction(OperationCode.IncScopeDepth, blockNode.LineNumber);
            _blockDepth++;

            foreach (var statement in blockNode.Statements)
            {
                CompileTargetNode(statement);
            }

            _blockDepth--;
            _chunk.AddInstruction(OperationCode.DecScopeDepth, blockNode.LineNumber);
        }

        #region Statement Compilation

        void CompileVarAssign(VarAssignNode varAssignNode)
        {
            /* [NOTE]
             * 
             * Assume that before compilation, variable semantics have been verified, and there are no name 
             * conflicts, and no unknown identifiers. Semantic analysis occurs between parsing and compilation.
             * 
             * [HOW VARIABLE ASSIGNMENTS WORK]
             * 
             * Assignments and declarations share syntax because the virtual machine handles them similarly due to 
             * dynamic typing. Here is how the VirtualMachine runs an assignment operation: 
             * 
             * 1. Pop a value off the stack representing the new/initial value for the variable. The new/initial value 
             *    is stored in a TaggedUnion and represents an expression evaluated at runtime. This can be of any type.
             *    
             * 2. Use the current Operation.Operand to get the variable's name stored as a string inside Chunk during 
             *    compile-time (i.e., the compilation logic code below). It's stored this way so Operations don't have 
             *    to store the identifiers themselves.
             *    
             * 3. Maps the new/initial value to the variable name in VirtualMachine's Dictionary<string, TaggedUnion> 
             *    field. If the name is already a key in the dictionary, then overwrite the existing value with the 
             *    new/initial value. 
             */

            CompileTargetNode(varAssignNode.Expression);

            // If a variable with the same name already exists in the chunk, the index of the existing variable will be returned.
            // Otherwise, the new variable will be added to the chunk and its new index will be returned.
            var varNameOperand = _chunk.RegisterVariableName(varAssignNode.Name);
            _chunk.AddInstruction(OperationCode.AssignOrDeclareVariable, varAssignNode.LineNumber, varNameOperand);
        }

        void CompileVarFactor(NameNode varFactorNode)
        {
            // Register to have its own constant in case the variable with this name is declared in a previous environment
            var varNameOperand = _chunk.RegisterVariableName(varFactorNode.Name);
            _chunk.AddInstruction(OperationCode.PushVariableValue, varFactorNode.LineNumber, varNameOperand);
        }

        void CompileReturn(ReturnNode returnNode)
        {
            if (returnNode.Expression != null)
            {
                CompileTargetNode(returnNode.Expression);
            }
            else
            {
                // Bare `return` returns None; ReturnValue always pops exactly one value off the stack.
                var noneIdx = _chunk.RegisterConstant(TaggedUnion.None);
                _chunk.AddInstruction(OperationCode.PushConstant, returnNode.LineNumber, noneIdx);
            }

            _chunk.AddInstruction(code: OperationCode.ReturnValue, returnNode.LineNumber);
        }

        void CompileExprStmnt(ExprStatementNode exprStmtNode)
        {
            CompileTargetNode(exprStmtNode.Expression);
            _chunk.AddInstruction(OperationCode.PopExprStmntResult, exprStmtNode.LineNumber);
        }

        void CompileIfStmnt(IfNode ifNode)
        {
            // Save outer chain's pending end-jumps so nested ifs don't corrupt it
            var saved = _pendingEndJumps;
            _pendingEndJumps = new List<int>();

            CompileTargetNode(ifNode.Expr);

            _chunk.AddInstruction(OperationCode.JumpIfFalse, ifNode.LineNumber);
            var jumpFalseIdx = _chunk.InstructionCount - 1;

            CompileTargetNode(ifNode.Block);

            // Only emit a jump-past-branches if there's actually a branch to skip
            if (ifNode.Branch != null)
            {
                _chunk.AddInstruction(OperationCode.JumpPastBranches, ifNode.LineNumber);
                _pendingEndJumps.Add(_chunk.InstructionCount - 1);
            }

            // JumpIfFalse lands at the start of the next branch (or END if no branch)
            _chunk.PatchInstruction(jumpFalseIdx, _chunk.InstructionCount);

            if (ifNode.Branch != null)
            {
                CompileTargetNode(ifNode.Branch);
            }

            // Patch every JumpPastBranches in this chain to land at END (current count)
            foreach (var idx in _pendingEndJumps)
            {
                _chunk.PatchInstruction(idx, _chunk.InstructionCount);
            }

            _pendingEndJumps = saved;
        }

        void CompileWhileStmnt(WhileNode whileNode)
        {
            // loopStart marks the start of the condition; both `continue` and the bottom-of-body Loop op target it.
            var loopStartIdx = _chunk.InstructionCount;

            CompileTargetNode(whileNode.Expr);

            _chunk.AddInstruction(OperationCode.JumpIfFalse, whileNode.LineNumber);
            var exitJumpIdx = _chunk.InstructionCount - 1;

            var ctx = new LoopContext
            {
                LoopStartIdx = loopStartIdx,
                BlockDepthAtEntry = _blockDepth,
            };

            _loopCtxStack.Push(ctx);

            CompileTargetNode(whileNode.Block);

            _loopCtxStack.Pop();

            _chunk.AddInstruction(OperationCode.Loop, whileNode.LineNumber, loopStartIdx);

            // Condition-false exit and any `break` jumps land here, after the backward Loop.
            var exitIdx = _chunk.InstructionCount;
            _chunk.PatchInstruction(exitJumpIdx, exitIdx);

            foreach (var idx in ctx.PendingBreaks)
            {
                _chunk.PatchInstruction(idx, exitIdx);
            }
        }

        void CompileBreakStmnt(BreakNode breakNode)
        {
            if (_loopCtxStack.Count == 0)
            {
                throw new ParserEx("'break' outside loop", breakNode.LineNumber);
            }

            var ctx = _loopCtxStack.Peek();
            EmitScopeExits(ctx.BlockDepthAtEntry, breakNode.LineNumber);

            _chunk.AddInstruction(OperationCode.JumpPastBranches, breakNode.LineNumber);
            ctx.PendingBreaks.Add(_chunk.InstructionCount - 1);
        }

        void CompileContinueStmnt(ContinueNode continueNode)
        {
            if (_loopCtxStack.Count == 0)
            {
                // TODO: Remove ParserEx in the compiler and replace with a more appropriate exception type
                throw new ParserEx("'continue' not properly in loop", continueNode.LineNumber);
            }

            var ctx = _loopCtxStack.Peek();
            EmitScopeExits(ctx.BlockDepthAtEntry, continueNode.LineNumber);

            _chunk.AddInstruction(OperationCode.Loop, continueNode.LineNumber, ctx.LoopStartIdx);
        }

        // break/continue jump past the textual DecScopeDepth instructions of every block they escape;
        // emit one DecScopeDepth per escaped level so the VM's scope stack stays balanced.
        void EmitScopeExits(int targetDepth, int lineNum)
        {
            var levels = _blockDepth - targetDepth;
            for (var i = 0; i < levels; i++)
            {
                _chunk.AddInstruction(OperationCode.DecScopeDepth, lineNum);
            }
        }

        void CompileBranchStmnt(BranchStmntNode node)
        {
            if (node.IsElse)
            {
                CompileTargetNode(node.Block);
                return;
            }

            CompileTargetNode(node.Expr);

            _chunk.AddInstruction(OperationCode.JumpIfFalse, node.LineNumber);
            var jumpFalseIdx = _chunk.InstructionCount - 1;

            CompileTargetNode(node.Block);

            if (node.Branch != null)
            {
                _chunk.AddInstruction(OperationCode.JumpPastBranches, node.LineNumber);
                _pendingEndJumps.Add(_chunk.InstructionCount - 1);
            }

            _chunk.PatchInstruction(jumpFalseIdx, _chunk.InstructionCount);

            if (node.Branch != null)
            {
                CompileTargetNode(node.Branch);
            }
        }

        #endregion

        #region Expression Compilation

        void CompileExpression(ExprNode exprNode)
        {
            // `and`/`or` short-circuit, so they cannot use the eager postfix layout used by all other binary operators
            if (exprNode.Operator == ExprOperator.And || exprNode.Operator == ExprOperator.Or)
            {
                CompileShortCircuit(exprNode);
                return;
            }

            // Compile operands first so they are pushed onto the runtime stack before the operation consumes them
            CompileTargetNode(exprNode.Left);

            if (exprNode.Right != null)
            {
                CompileTargetNode(exprNode.Right);
            }

            var opCode = GetExpressionOperationCode(exprNode);
            _chunk.AddInstruction(opCode, exprNode.LineNumber);
        }

        void CompileShortCircuit(ExprNode node)
        {
            CompileTargetNode(node.Left);

            OperationCode jumpCode;

            if (node.Operator == ExprOperator.And)
            {
                jumpCode = OperationCode.JumpIfFalseOrPop;
            }
            else
            {
                jumpCode = OperationCode.JumpIfTrueOrPop;
            }

            // Emit jump with placeholder operand; the real target is unknown until the right side is compiled
            _chunk.AddInstruction(jumpCode, node.LineNumber);
            var patchIdx = _chunk.InstructionCount - 1;

            CompileTargetNode(node.Right);

            // Land just past the right-hand bytecode
            _chunk.PatchInstruction(patchIdx, _chunk.InstructionCount);
        }

        static OperationCode GetExpressionOperationCode(ExprNode node)
        {
            OperationCode opCode;
            switch (node.Operator)
            {
                case ExprOperator.Add:
                    opCode = OperationCode.Add;
                    break;

                case ExprOperator.Subtract:
                    opCode = OperationCode.Subtract;
                    break;

                case ExprOperator.Multiply:
                    opCode = OperationCode.Multiply;
                    break;

                case ExprOperator.Divide:
                    opCode = OperationCode.Divide;
                    break;

                case ExprOperator.Modulus:
                    opCode = OperationCode.Modulus;
                    break;

                case ExprOperator.Exponentiate:
                    opCode = OperationCode.Exponentiate;
                    break;

                case ExprOperator.FloorDivide:
                    opCode = OperationCode.FloorDivide;
                    break;

                case ExprOperator.Negate:
                    opCode = OperationCode.Negate;
                    break;

                case ExprOperator.Equal:
                    opCode = OperationCode.Equal;
                    break;

                case ExprOperator.NotEqual:
                    opCode = OperationCode.NotEqual;
                    break;

                case ExprOperator.Less:
                    opCode = OperationCode.Less;
                    break;

                case ExprOperator.Greater:
                    opCode = OperationCode.Greater;
                    break;

                case ExprOperator.LessEqual:
                    opCode = OperationCode.LessEqual;
                    break;

                case ExprOperator.GreaterEqual:
                    opCode = OperationCode.GreaterEqual;
                    break;

                case ExprOperator.Not:
                    opCode = OperationCode.Not;
                    break;

                case ExprOperator.BinaryOr:
                    opCode = OperationCode.BinaryOr;
                    break;

                case ExprOperator.In:
                    opCode = OperationCode.In;
                    break;

                case ExprOperator.NotIn:
                    opCode = OperationCode.NotIn;
                    break;

                default:
                    throw new NotImplementedException(nameof(node.Operator));
            }

            return opCode;
        }

        void CompileLiteral(LiteralNode literalNode)
        {

            var constUnion = TaggedUnion.Empty;

            switch (literalNode.Type)
            {
                case LiteralDataType.Integer:
                    // Cases for LiteralDataType like this should not fail unless the Parser is bugged
                    if (literalNode.Value is long intVal)
                    {
                        constUnion = new TaggedUnion(intVal);
                    }
                    break;

                case LiteralDataType.Float:
                    if (literalNode.Value is double floatVal)
                    {
                        constUnion = new TaggedUnion(floatVal);
                    }
                    break;

                case LiteralDataType.Boolean:
                    if (literalNode.Value is bool boolVal)
                    {
                        constUnion = new TaggedUnion(boolVal);
                    }
                    break;

                case LiteralDataType.None:
                    constUnion = TaggedUnion.None;
                    break;

                case LiteralDataType.String:
                    if (literalNode.Value is string strVal)
                    {
                        constUnion = new TaggedUnion(strVal);
                    }
                    break;

                default:
                    throw new NotImplementedException($"Compilation of literal type {literalNode.Type} is not implemented.");
            }

            if (constUnion.IsEmpty)
            {
                // This case should never occur unless the Parser is bugged. Refer to the inline comment above for more info
                throw new InvalidOperationException();
            }

            // If a constant of the same value already exists in the chunk, the operand of the existing constant will be returned.
            // Otherwise, the new constant will be added to the chunk and its new operand will be returned.
            var constIdx = _chunk.RegisterConstant(constUnion);
            _chunk.AddInstruction(OperationCode.PushConstant, literalNode.LineNumber, constIdx);
        }

        #endregion


        sealed class LoopContext
        {
            public int LoopStartIdx;
            public int BlockDepthAtEntry;
            public List<int> PendingBreaks = new List<int>();
        }

    }
}