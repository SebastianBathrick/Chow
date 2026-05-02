using Chow.Syntax;
using Chow.Tokens;
using System;
using System.Collections.Generic;

namespace Chow.Syntax
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
                left = new ExpressionNode(MapBinary(opToken.Type), left, right, opToken.LineNum);
            }

            return left;
        }

        Node ParseTerm()
        {
            Node left = ParseFactor();

            while (Match(TokenType.Star, TokenType.Slash, TokenType.SlashSlash, TokenType.Percent))
            {
                Token opToken = _tokens[_tokenIndex - 1];
                Node right = ParseFactor();
                left = new ExpressionNode(MapBinary(opToken.Type), left, right, opToken.LineNum);
            }

            return left;
        }

        Node ParseFactor()
        {
            if (Match(TokenType.Minus))
            {
                Token opToken = _tokens[_tokenIndex - 1];
                return new ExpressionNode(ExpressionOperator.Negate, ParseFactor(), opToken.LineNum);
            }

            return ParseExponent();
        }

        Node ParseExponent()
        {
            Node left = ParsePrimary();

            if (Match(TokenType.StarStar))
            {
                Token opToken = _tokens[_tokenIndex - 1];
                Node right = ParseFactor();
                return new ExpressionNode(ExpressionOperator.Exponentiate, left, right, opToken.LineNum);
            }

            return left;
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
                    return new LiteralNode(token.Literal, token.LineNum);
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

        bool Match(TokenType typeA, TokenType typeB, TokenType typeC, TokenType typeD)
        {
            if (Check(typeA) || Check(typeB) || Check(typeC) || Check(typeD))
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

        static ExpressionOperator MapBinary(TokenType type)
        {
            switch (type)
            {
                case TokenType.Plus:
                    return ExpressionOperator.Add;

                case TokenType.Minus:
                    return ExpressionOperator.Subtract;

                case TokenType.Star:
                    return ExpressionOperator.Multiply;

                case TokenType.Slash:
                    return ExpressionOperator.Divide;

                case TokenType.Percent:
                    return ExpressionOperator.Modulus;

                case TokenType.StarStar:
                    return ExpressionOperator.Exponentiate;

                case TokenType.SlashSlash:
                    return ExpressionOperator.FloorDivide;

                default:
                    throw new InvalidOperationException($"Unexpected binary operator: {type}");
            }
        }
    }
}
