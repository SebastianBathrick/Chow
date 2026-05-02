using Chow.Syntax;
using Chow.Tokens;
using System;
using System.Collections.Generic;

namespace Chow.Parsing
{
    class Parser
    {
        List<Token> _tokens;
        int _tokenIndex;
        bool _isDirty;

        private Token CurrentToken => _tokens[_tokenIndex];

        public Parser(List<Token> tokens)
        {
            _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        }

        // ============================================================================================================
        // Primary Methods
        // ============================================================================================================

        public Node BuildSyntaxTree()
        {
            if (!_isDirty)
            {
                _isDirty = true;
            }
            else
            {
                throw new InvalidOperationException("This Parser instance can only be used once.");
            }

            Node root = ParseExpression();

            if (Check(TokenType.Newline))
            {
                MoveToNextToken();
            }

            Consume(TokenType.EndOfCode, "Expected end of expression.");
            return root;
        }

        // ============================================================================================================
        // Grammar Methods
        // ============================================================================================================

        Node ParseExpression()
        {
            Node left = ParseTerm();

            while (Match(TokenType.Plus, TokenType.Minus))
            {
                Token opToken = _tokens[_tokenIndex - 1];
                Node right = ParseTerm();
                left = new ExpressionOperationNode(MapBinary(opToken.Type), left, right);
            }

            return left;
        }

        Node ParseTerm()
        {
            Node left = ParseFactor();

            while (Match(TokenType.Star, TokenType.Slash))
            {
                Token opToken = _tokens[_tokenIndex - 1];
                Node right = ParseFactor();
                left = new ExpressionOperationNode(MapBinary(opToken.Type), left, right);
            }

            return left;
        }

        Node ParseFactor()
        {
            if (Match(TokenType.Minus))
            {
                return new ExpressionOperationNode(ExpressionOperationNode.OperatorType.Negate, ParseFactor());
            }

            return ParsePrimary();
        }

        Node ParsePrimary()
        {
            switch (CurrentToken.Type)
            {
                case TokenType.Integer:
                case TokenType.Float:
                {
                    Token token = CurrentToken;
                    MoveToNextToken();
                    return new LiteralNode(token.Literal);
                }

                case TokenType.LeftParenthesis:
                {
                    MoveToNextToken();
                    Node inner = ParseExpression();
                    Consume(TokenType.RightParenthesis, "Expected ')' after expression.");
                    return inner;
                }

                default:
                    throw new ParserException("Expected expression.", CurrentToken.LineNum);
            }
        }

        // ============================================================================================================
        // Token Helpers
        // ============================================================================================================

        void MoveToNextToken()
        {
            if (CurrentToken.Type != TokenType.EndOfCode)
            {
                _tokenIndex++;
            }
        }

        bool Check(TokenType type)
        {
            return CurrentToken.Type == type;
        }

        bool Match(TokenType type)
        {
            if (Check(type))
            {
                MoveToNextToken();
                return true;
            }

            return false;
        }

        bool Match(TokenType typeA, TokenType typeB)
        {
            if (Check(typeA) || Check(typeB))
            {
                MoveToNextToken();
                return true;
            }

            return false;
        }

        Token Consume(TokenType type, string message)
        {
            if (Check(type))
            {
                Token token = CurrentToken;
                MoveToNextToken();
                return token;
            }

            throw new ParserException(message, CurrentToken.LineNum);
        }

        // ============================================================================================================
        // Helper Methods
        // ============================================================================================================

        static ExpressionOperationNode.OperatorType MapBinary(TokenType type)
        {
            switch (type)
            {
                case TokenType.Plus:
                    return ExpressionOperationNode.OperatorType.Add;
                case TokenType.Minus:
                    return ExpressionOperationNode.OperatorType.Subtract;
                case TokenType.Star:
                    return ExpressionOperationNode.OperatorType.Multiply;
                case TokenType.Slash:
                    return ExpressionOperationNode.OperatorType.Divide;
                default:
                    throw new InvalidOperationException($"Unexpected binary operator: {type}");
            }
        }
    }
}
