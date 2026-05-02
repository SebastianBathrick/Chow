using Chow.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Bytecode
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

        public Chunk CompileSyntaxTree()
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
                case LiteralNode literalNode:
                    CompileLiteral(literalNode);
                    break;

                case ExpressionNode expressionNode:
                    CompileExpression(expressionNode);
                    break;

                default:
                    throw new NotImplementedException($"Compilation of {targetNode.GetType().Name} is not implemented.");
            }
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

            int constIndex = _chunk.AddConstant(constUnion);
            _chunk.PushOperation(OperationCode.PushConstant, literalNode.LineNumber, constIndex);
        }


        void CompileExpression(ExpressionNode expressionNode)
        {
            // Note: This specific stack order will allow for short circuiting
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

                case ExpressionOperator.Negate:
                    opCode = OperationCode.Negate;
                    break;

                default:
                    throw new NotImplementedException($"Compilation of operator type {node.Operator} is not implemented.");
            }

            return opCode;
        }
    }
}