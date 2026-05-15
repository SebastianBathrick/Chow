using Chow.Interpreter.Bytecode;
using Chow.Interpreter.Exceptions;
using Chow.Interpreter.State.Values;
using System.Collections.Generic;
using Chow.Interpreter.SyntaxTrees;
using Chow.Interpreter.SyntaxTrees.Expressions;
using Chow.Interpreter.SyntaxTrees.Statements;
using System;

namespace Chow.Interpreter
{
    class Compiler
    {
        readonly Chunk _chunk;
        readonly Node _root;
        readonly Stack<LoopContext> _loopContextStack;
        
        List<int> _pendingEndJumps;

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

        Chunk CompileFuncBody()
        {
            if (!(_root is FunctionNode funcNode))
            {
                throw new InvalidOperationException();
            }
            
            // Caller pushes args left-to-right; bind in reverse so positional order matches when popping
            for (var i = funcNode.Params.Count - 1; i >= 0; i--)
            {
                var param = (NameNode)funcNode.Params[i];
                var pramConstIdx = _chunk.RegisterVariableName(param.Name);

                _chunk.AddInstruction(OperationCode.VariableAssignOrDeclare, param.LineNumber, pramConstIdx);
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

            // Push template, then runtime CreateClosureFromTemplate captures the active scope and wraps it as a Closure.
            _chunk.AddInstruction(OperationCode.PushConstant, funcNode.LineNumber, templateIdx);
            _chunk.AddInstruction(OperationCode.CreateClosureFromTemplate, funcNode.LineNumber);

            // Functions work like variables, can be reassigned, and be passed around as values.
            // This method represents something similar in concept to VariableAssignOrDeclare, but for functions.
            var varNameIdx = _chunk.RegisterVariableName(funcNode.Name);
            _chunk.AddInstruction(OperationCode.VariableAssignOrDeclare, funcNode.LineNumber, varNameIdx);
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

                case ExprNode exprNode:
                    CompileExpression(exprNode);
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
                    // TODO: Check if CompileListLiteral & CompileDictLiteral should be grouped with all the other literals
                    CompileListLiteral(listLiteralNode);
                    break;

                case DictLiteralNode dictLiteralNode:
                    CompileDictLiteral(dictLiteralNode);
                    break;

                case SubscriptNode subscrNode:
                    CompileSubscript(subscrNode);
                    break;

                case AttrAccessNode attrAccessNode:
                    CompileAttrAccess(attrAccessNode);
                    break;

                case SubscriptAssignNode subscrAssignNode:
                    CompileSubscriptAssign(subscrAssignNode);
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

            _chunk.AddInstruction(OperationCode.CreateInternalList, node.LineNumber, node.Elements.Count);
        }

        void CompileDictLiteral(DictLiteralNode node)
        {
            for (var i = 0; i < node.Keys.Count; i++)
            {
                CompileTargetNode(node.Keys[i]);
                CompileTargetNode(node.Values[i]);
            }

            _chunk.AddInstruction(OperationCode.CreateInternalDict, node.LineNumber, node.Keys.Count);
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
            var varNameIdx = _chunk.RegisterVariableName(node.AttrName);
            _chunk.AddInstruction(OperationCode.GetVariableAttribute, node.LineNumber, varNameIdx);
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

            var varNameIdx = _chunk.RegisterVariableName(node.AttrName);
            _chunk.AddInstruction(OperationCode.SetVariableAttribute, node.LineNumber, varNameIdx);
        }

        void CompileBlockNode(BlockNode blockNode)
        {
            // Python has no block scope: names assigned inside an `if`/`while` body
            // belong to the enclosing function or module scope, so no Inc/DecScopeDepth.
            foreach (var statement in blockNode.Statements)
            {
                CompileTargetNode(statement);
            }
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
            var varNameIdx = _chunk.RegisterVariableName(varAssignNode.Name);
            _chunk.AddInstruction(OperationCode.VariableAssignOrDeclare, varAssignNode.LineNumber, varNameIdx);
        }

        void CompileVarFactor(NameNode varFactorNode)
        {
            // Register to have its own constant in case the variable with this name is declared in a previous environment
            var varNameIdx = _chunk.RegisterVariableName(varFactorNode.Name);
            _chunk.AddInstruction(OperationCode.VariablePushValue, varFactorNode.LineNumber, varNameIdx);
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
            _chunk.AddInstruction(OperationCode.PopExpressionStatementResult, exprStmtNode.LineNumber);
        }

        void CompileIfStmnt(IfNode ifNode)
        {
            // Save outer chain's pending end-jumps so nested ifs don't corrupt it
            var saved = _pendingEndJumps;

            // TODO: Check if we can avoid using a new list here and just clear the existing one
            _pendingEndJumps = new List<int>();
            CompileTargetNode(ifNode.Expr);

            _chunk.AddInstruction(OperationCode.JumpIfFalse, ifNode.LineNumber);
            var jumpFalseIdx = _chunk.InstructionCount - 1;

            CompileTargetNode(ifNode.Block);

            // Only emit a jump-past-branches if there's actually a branch to skip
            if (ifNode.Branch != null)
            {
                _chunk.AddInstruction(OperationCode.JumpPastElseBranches, ifNode.LineNumber);
                _pendingEndJumps.Add(_chunk.InstructionCount - 1);
            }

            // JumpIfFalse lands at the start of the next branch (or END if no branch)
            _chunk.PatchInstructionOperand(jumpFalseIdx, _chunk.InstructionCount);

            if (ifNode.Branch != null)
            {
                CompileTargetNode(ifNode.Branch);
            }

            // Patch every JumpPastElseBranches in this chain to land at END (current count)
            foreach (var idx in _pendingEndJumps)
            {
                _chunk.PatchInstructionOperand(idx, _chunk.InstructionCount);
            }

            _pendingEndJumps = saved;
        }

        void CompileWhileStmnt(WhileNode whileNode)
        {
            // loopStart marks the start of the condition; both `continue` and the bottom-of-body JumpToLoopStart op target it.
            var loopStartIdx = _chunk.InstructionCount;

            CompileTargetNode(whileNode.Expr);

            _chunk.AddInstruction(OperationCode.JumpIfFalse, whileNode.LineNumber);
            var exitJumpIdx = _chunk.InstructionCount - 1;

            var loopContext = new LoopContext
            {
                LoopStartIdx = loopStartIdx,
            };

            _loopContextStack.Push(loopContext);

            CompileTargetNode(whileNode.Block);

            _loopContextStack.Pop();

            _chunk.AddInstruction(OperationCode.JumpToLoopStart, whileNode.LineNumber, loopStartIdx);

            // Condition-false exit and any `break` jumps land here, after the backward JumpToLoopStart.
            var exitIdx = _chunk.InstructionCount;
            _chunk.PatchInstructionOperand(exitJumpIdx, exitIdx);

            foreach (var idx in loopContext.PendingBreaks)
            {
                _chunk.PatchInstructionOperand(idx, exitIdx);
            }
        }

        void CompileBreakStmnt(BreakNode breakNode)
        {
            if (_loopContextStack.Count == 0)
            {
                throw new ParserEx("'break' outside loop", breakNode.LineNumber);
            }

            var loopContext = _loopContextStack.Peek();

            _chunk.AddInstruction(OperationCode.JumpPastElseBranches, breakNode.LineNumber);
            loopContext.PendingBreaks.Add(_chunk.InstructionCount - 1);
        }

        void CompileContinueStmnt(ContinueNode continueNode)
        {
            if (_loopContextStack.Count == 0)
            {
                // TODO: Remove ParserEx in the compiler and replace with a more appropriate exception type
                throw new ParserEx("'continue' not properly in loop", continueNode.LineNumber);
            }

            var loopContext = _loopContextStack.Peek();

            _chunk.AddInstruction(OperationCode.JumpToLoopStart, continueNode.LineNumber, loopContext.LoopStartIdx);
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
                _chunk.AddInstruction(OperationCode.JumpPastElseBranches, node.LineNumber);
                _pendingEndJumps.Add(_chunk.InstructionCount - 1);
            }

            _chunk.PatchInstructionOperand(jumpFalseIdx, _chunk.InstructionCount);

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

        void CompileShortCircuit(ExprNode exprNode)
        {
            CompileTargetNode(exprNode.Left);

            OperationCode jumpCode;

            if (exprNode.Operator == ExprOperator.And)
            {
                jumpCode = OperationCode.JumpIfFalseOrPop;
            }
            else
            {
                jumpCode = OperationCode.JumpIfTrueOrPop;
            }

            // Emit jump with placeholder operand; the real target is unknown until the right side is compiled
            _chunk.AddInstruction(jumpCode, exprNode.LineNumber);
            var patchIdx = _chunk.InstructionCount - 1;

            CompileTargetNode(exprNode.Right);

            // Land just past the right-hand bytecode
            _chunk.PatchInstructionOperand(patchIdx, _chunk.InstructionCount);
        }

        static OperationCode GetExpressionOperationCode(ExprNode exprNode)
        {
            OperationCode opCode;
            
            switch (exprNode.Operator)
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
                    throw new NotImplementedException(nameof(exprNode.Operator));
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
            public readonly List<int> PendingBreaks = new List<int>();
        }

    }
}