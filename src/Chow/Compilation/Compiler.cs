using Chow.Interpreter.Syntax.Trees;
using Chow.Interpreter.Syntax.Trees.Expressions;
using Chow.Interpreter.Syntax.Trees.Statements;
using Chow.Interpreter.Values;
using System;

namespace Chow.Interpreter.Compilation
{
    class Compiler
    {
        Chunk _chunk;
        Node _root;

        public Compiler(Node root)
        {
            _chunk = new Chunk();
            _root = root;
        }

        public Chunk CompileSyntaxTreeRoot()
        {
            CompileTargetNode(_root);

            return _chunk;
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
                // TODO: Remove EmptyNode
                case EmptyNode _:
                    break;

                case RootNode root:
                    CompileSyntaxTreeRoot(root);
                    break;

                case BlockNode blockNode:
                    CompileBlockNode(blockNode);
                    break;

                case LiteralNode literalNode:
                    CompileLiteral(literalNode);
                    break;

                case ExprNode expressionNode:
                    CompileExpression(expressionNode);
                    break;

                case VariableAssignNode varAssignNode:
                    CompileVariableAssign(varAssignNode);
                    break;

                case NameNode varFactorNode:
                    CompileVariableFactor(varFactorNode);
                    break;

                case ReturnNode returnNode:
                    // If it returns early, still parse the remaining code in the chunk for debugging (subject to change)
                    CompileReturn(returnNode);
                    break;

                case ExprStatementNode exprStmtNode:
                    CompileExpressionStatement(exprStmtNode);
                    break;

                default:
                    throw new NotImplementedException($"Compilation of {targetNode.GetType().Name} is not implemented.");
            }
        }

        void CompileSyntaxTreeRoot(RootNode root)
        {
            CompileTargetNode(root.Module);
        }

        void CompileBlockNode(BlockNode blockNode)
        {
            foreach (Node statement in blockNode.Statements)
            {
                CompileTargetNode(statement);
            }
        }

        #region Statement Compilation

        void CompileVariableAssign(VariableAssignNode varAssignNode)
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
            _chunk.AddInstruction(OperationCode.AssignOrDeclareVariable, varAssignNode.LineNum, varNameOperand);
        }

        void CompileVariableFactor(NameNode varFactorNode)
        {
            // Register to have its own constant in case the variable with this name is declared in a previous environment
            int varNameOperand = _chunk.RegisterVariableName(varFactorNode.Name);
            _chunk.AddInstruction(OperationCode.PushVariableValue, varFactorNode.LineNum, varNameOperand);
        }

        void CompileReturn(ReturnNode returnNode)
        {
            if (returnNode.Expression != null)
            {
                CompileTargetNode(returnNode.Expression);
            }

            _chunk.AddInstruction(code: OperationCode.ReturnValue, returnNode.LineNum);
        }

        void CompileExpressionStatement(ExprStatementNode exprStmtNode)
        {
            CompileTargetNode(exprStmtNode.Expression);
            _chunk.AddInstruction(OperationCode.PopExprStmntResult, exprStmtNode.LineNum);
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
            _chunk.AddInstruction(opCode, exprNode.LineNum);
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
            _chunk.AddInstruction(jumpCode, node.LineNum);
            int patchIdx = _chunk.Count - 1;

            CompileTargetNode(node.Right);

            // Land just past the right-hand bytecode
            _chunk.PatchInstructionOperand(patchIdx, _chunk.Count);
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
            _chunk.AddInstruction(OperationCode.PushConstant, literalNode.LineNum, constIdx);
        }

        #endregion
    }
}