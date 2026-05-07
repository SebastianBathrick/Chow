using Chow.Interpreter.Syntax.Trees;
using Chow.Interpreter.Syntax.Trees.Expressions;
using Chow.Interpreter.Syntax.Trees.Statements;
using Chow.Interpreter.Values;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Interpreter.Compilation
{
    class Compiler
    {
        Chunk _chunk;
        Node _syntaxTreeRoot;
        bool _isDirty = false;

        public Compiler(Node syntaxTreeRoot)
        {
            if (syntaxTreeRoot == null)
            {
                throw new ArgumentNullException(nameof(syntaxTreeRoot));
            }

            _chunk = new Chunk();
            _syntaxTreeRoot = syntaxTreeRoot;
        }

        public Chunk CompileSyntaxTreeRoot()
        {
            if (_isDirty)
            {
                throw new InvalidOperationException("This Compiler instance can only be used once");
            }

            _isDirty = true;
            CompileTargetNode(_syntaxTreeRoot);

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
                case EmptyNode _:
                    break;

                case SyntaxTreeRoot root:
                    CompileSyntaxTreeRoot(root);
                    break;

                case BlockNode blockNode:
                    CompileBlockNode(blockNode);
                    break;

                case LiteralNode literalNode:
                    CompileLiteral(literalNode);
                    break;

                case ExpressionNode expressionNode:
                    CompileExpression(expressionNode);
                    break;

                case VariableAssignNode varAssignNode:
                    CompileVariableAssign(varAssignNode);
                    break;

                case VariableFactorNode varFactorNode:
                    CompileVariableFactor(varFactorNode);
                    break;

                default:
                    throw new NotImplementedException($"Compilation of {targetNode.GetType().Name} is not implemented.");
            }
        }

        void CompileSyntaxTreeRoot(SyntaxTreeRoot root)
        {
            CompileTargetNode(root.TopLevelBlock);
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
            _chunk.PushOperation(OperationCode.AssignOrDeclareVariable, varAssignNode.LineNumber, varNameOperand);
        }

        void CompileVariableFactor(VariableFactorNode varFactorNode)
        {
            int varNameOperand = _chunk.FindVariableName(varFactorNode.VariableName);
            _chunk.PushOperation(OperationCode.PushVariableValue, varFactorNode.LineNumber, varNameOperand);
        }

        void CompileReturn(ReturnNode returnNode)
        {
            if (returnNode.Expression != null)
            {
                CompileTargetNode(returnNode.Expression);
            }

            _chunk.PushOperation(operationType: OperationCode.ReturnValue, returnNode.LineNumber);

        }

        #endregion

        #region Expression Compilation

        void CompileExpression(ExpressionNode expressionNode)
        {
            // Compile operands first so they are pushed onto the runtime stack before the operation consumes them
            CompileTargetNode(expressionNode.Left);
            CompileTargetNode(expressionNode.Right);

            OperationCode opCode = GetExpressionOperationCode(expressionNode);
            _chunk.PushOperation(opCode, expressionNode.LineNumber);
        }
        
        private static OperationCode GetExpressionOperationCode(ExpressionNode node)
        {
            OperationCode opCode;
            switch (node.Operator)
            {
                case ExpressionOperator.Add:
                    opCode = OperationCode.Add;
                    break;

                case ExpressionOperator.Subtract:
                    opCode = OperationCode.Subtract;
                    break;

                case ExpressionOperator.Multiply:
                    opCode = OperationCode.Multiply;
                    break;

                case ExpressionOperator.Divide:
                    opCode = OperationCode.Divide;
                    break;

                case ExpressionOperator.Modulus:
                    opCode = OperationCode.Modulus;
                    break;

                case ExpressionOperator.Exponentiate:
                    opCode = OperationCode.Exponentiate;
                    break;

                case ExpressionOperator.FloorDivide:
                    opCode = OperationCode.FloorDivide;
                    break;

                case ExpressionOperator.Negate:
                    opCode = OperationCode.Negate;
                    break;

                default:
                    throw new NotImplementedException($"Compilation of operator type {node.Operator} is not implemented.");
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
            int constIndex = _chunk.RegisterConstant(constUnion);
            _chunk.PushOperation(OperationCode.PushConstant, literalNode.LineNumber, constIndex);
        }

        #endregion
    }
}