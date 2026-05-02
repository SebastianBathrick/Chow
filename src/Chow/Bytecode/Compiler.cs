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

        public Compiler(Node syntaxTreeRoot)
        {
            if (syntaxTreeRoot == null)
            {
                throw new ArgumentNullException(nameof(syntaxTreeRoot));
            }

            _chunk = new Chunk();

            _syntaxTreeRoot = syntaxTreeRoot;
        }

        public void CompileTargetNode(Node targetNode)
        {
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