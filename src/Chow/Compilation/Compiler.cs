using Chow.Interpreter.Evaluation;
using Chow.Interpreter.Syntax.Trees;
using Chow.Interpreter.Syntax.Trees.Expressions;
using Chow.Interpreter.Syntax.Trees.Statements;
using Chow.Interpreter.Values.Internal;
using System;
using System.Collections.Generic;

namespace Chow.Interpreter.Compilation
{
    class Compiler
    {
        Chunk _chunk;
        Node _root;
        List<int> _pendingEndJumps;

        public Compiler(Node root)
        {
            _chunk = new Chunk();
            _root = root;
            _pendingEndJumps = new List<int>();
        }

        public Chunk CompileRoot()
        {
            TreeRootNode treeRoot = _root as TreeRootNode;

            foreach (Node stmnt in treeRoot.Stmnts)
            {
                CompileTargetNode(stmnt);
            }

            return _chunk;
        }

        Chunk CompileFuncBody()
        {
            FunctionNode funcNode = _root as FunctionNode;

            // Caller pushes args left-to-right; bind in reverse so positional order matches when popping
            for (int i = funcNode.Params.Count - 1; i >= 0; i--)
            {
                NameNode param = (NameNode)funcNode.Params[i];
                int paramOperand = _chunk.RegisterVariableName(param.Name);
                _chunk.AddInstr(OperationCode.AssignOrDeclareVariable, param.LineNum, paramOperand);
            }

            CompileTargetNode(funcNode.Body);

            // Implicit `return None` for funcs that fall off the end of the body
            int noneIdx = _chunk.RegisterConstant(TaggedUnion.None);
            _chunk.AddInstr(OperationCode.PushConstant, funcNode.LineNum, noneIdx);
            _chunk.AddInstr(OperationCode.ReturnValue, funcNode.LineNum);

            return _chunk;
        }

        void CompileFuncDeclaration(FunctionNode funcNode)
        {
            Compiler funcCompiler = new Compiler(funcNode);
            Chunk funcChunk = funcCompiler.CompileFuncBody();

            ClosureTemplate template = new ClosureTemplate(funcChunk, funcNode.Name, funcNode.Params.Count);
            int templateIdx = _chunk.RegisterConstant(new TaggedUnion((object)template));

            // Push template, then runtime MakeClosure captures the active scope and wraps it as a Closure.
            _chunk.AddInstr(OperationCode.PushConstant, funcNode.LineNum, templateIdx);
            _chunk.AddInstr(OperationCode.MakeClosure, funcNode.LineNum);

            int nameIdx = _chunk.RegisterVariableName(funcNode.Name);
            _chunk.AddInstr(OperationCode.AssignOrDeclareVariable, funcNode.LineNum, nameIdx);
        }

        void CompileCall(CallNode callNode)
        {
            int nameOperand = _chunk.RegisterVariableName(callNode.Name);
            _chunk.AddInstr(OperationCode.PushVariableValue, callNode.LineNum, nameOperand);

            foreach (Node arg in callNode.Args)
            {
                CompileTargetNode(arg);
            }

            _chunk.AddInstr(OperationCode.Call, callNode.LineNum, callNode.Args.Count);
        }

        void CompileTargetNode(Node targetNode)
        {
            if (targetNode == null)
            {
                // This case occurs when at the end of a branch of the syntax tree
                return;
            }

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

                case FunctionNode funcNode:
                    CompileFuncDeclaration(funcNode);
                    break;

                case CallNode callNode:
                    CompileCall(callNode);
                    break;

                default:
                    throw new InvalidOperationException();
            }
        }

        void CompileBlockNode(BlockNode blockNode)
        {
            // Indicate that the scope's depth increased and all variables that follow are nested in this block
            _chunk.AddInstr(OperationCode.IncScopeDepth, blockNode.LineNum);

            foreach (Node statement in blockNode.Statements)
            {
                CompileTargetNode(statement);
            }

            _chunk.AddInstr(OperationCode.DecScopeDepth, blockNode.LineNum);
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
            int varNameOperand = _chunk.RegisterVariableName(varAssignNode.Name);
            _chunk.AddInstr(OperationCode.AssignOrDeclareVariable, varAssignNode.LineNum, varNameOperand);
        }

        void CompileVarFactor(NameNode varFactorNode)
        {
            // Register to have its own constant in case the variable with this name is declared in a previous environment
            int varNameOperand = _chunk.RegisterVariableName(varFactorNode.Name);
            _chunk.AddInstr(OperationCode.PushVariableValue, varFactorNode.LineNum, varNameOperand);
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
                int noneIdx = _chunk.RegisterConstant(TaggedUnion.None);
                _chunk.AddInstr(OperationCode.PushConstant, returnNode.LineNum, noneIdx);
            }

            _chunk.AddInstr(code: OperationCode.ReturnValue, returnNode.LineNum);
        }

        void CompileExprStmnt(ExprStatementNode exprStmtNode)
        {
            CompileTargetNode(exprStmtNode.Expression);
            _chunk.AddInstr(OperationCode.PopExprStmntResult, exprStmtNode.LineNum);
        }

        void CompileIfStmnt(IfNode ifNode)
        {
            // Save outer chain's pending end-jumps so nested ifs don't corrupt it
            List<int> saved = _pendingEndJumps;
            _pendingEndJumps = new List<int>();

            CompileTargetNode(ifNode.Expr);

            _chunk.AddInstr(OperationCode.JumpIfFalse, ifNode.LineNum);
            int jumpFalseIdx = _chunk.Count - 1;

            CompileTargetNode(ifNode.Block);

            // Only emit a jump-past-branches if there's actually a branch to skip
            if (ifNode.Branch != null)
            {
                _chunk.AddInstr(OperationCode.JumpPastBranches, ifNode.LineNum);
                _pendingEndJumps.Add(_chunk.Count - 1);
            }

            // JumpIfFalse lands at the start of the next branch (or END if no branch)
            _chunk.PatchInstrOperand(jumpFalseIdx, _chunk.Count);

            CompileTargetNode(ifNode.Branch);

            // Patch every JumpPastBranches in this chain to land at END (current count)
            foreach (int idx in _pendingEndJumps)
            {
                _chunk.PatchInstrOperand(idx, _chunk.Count);
            }

            _pendingEndJumps = saved;
        }

        void CompileBranchStmnt(BranchStmntNode node)
        {
            if (node.IsElse)
            {
                CompileTargetNode(node.Block);
                return;
            }

            CompileTargetNode(node.Expr);

            _chunk.AddInstr(OperationCode.JumpIfFalse, node.LineNum);
            int jumpFalseIdx = _chunk.Count - 1;

            CompileTargetNode(node.Block);

            if (node.Branch != null)
            {
                _chunk.AddInstr(OperationCode.JumpPastBranches, node.LineNum);
                _pendingEndJumps.Add(_chunk.Count - 1);
            }

            _chunk.PatchInstrOperand(jumpFalseIdx, _chunk.Count);

            CompileTargetNode(node.Branch);
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
            CompileTargetNode(exprNode.Right);

            OperationCode opCode = GetExpressionOperationCode(exprNode);
            _chunk.AddInstr(opCode, exprNode.LineNum);
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
            _chunk.AddInstr(jumpCode, node.LineNum);
            int patchIdx = _chunk.Count - 1;

            CompileTargetNode(node.Right);

            // Land just past the right-hand bytecode
            _chunk.PatchInstrOperand(patchIdx, _chunk.Count);
        }

        private static OperationCode GetExpressionOperationCode(ExprNode node)
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

                default:
                    throw new NotImplementedException(nameof(node.Operator));
            }

            return opCode;
        }

        void CompileLiteral(LiteralNode literalNode)
        {

            TaggedUnion constUnion = TaggedUnion.Empty;

            switch (literalNode.Type)
            {
                case LiteralDataType.Integer:
                    // Cases for LiteralDataType like this should not fail unless the Parser is bugged
                    if (literalNode.Value is int intVal)
                    {
                        constUnion = new TaggedUnion(intVal);
                    }
                    break;

                case LiteralDataType.Float:
                    if (literalNode.Value is float floatVal)
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
            int constIdx = _chunk.RegisterConstant(constUnion);
            _chunk.AddInstr(OperationCode.PushConstant, literalNode.LineNum, constIdx);
        }

        #endregion
    }
}