using Chow.Interpreter.Syntax.Trees.Expressions;
using Chow.Interpreter.Syntax.Trees.Statements;
using Chow.Interpreter.Syntax.Trees;
using Chow.Interpreter.Tokens;
using System.Collections.Generic;
using System;

namespace Chow.Interpreter.Syntax
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

        #region Primary Methods

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

            if (_tokens.Count == 0)
            {
                return new EmptyNode();
            }

            Node block = ParseBlock(isTopLevel: true);
            Consume(TokenType.EndOfCode, "Expected end of code.");
            return new SyntaxTreeRoot(block, block.LineNumber);
        }

        Node ParseBlock(bool isTopLevel = false)
        {
            int lineNumber;

            if (!isTopLevel)
            {
                lineNumber = Consume(TokenType.Indent, "Expected indent.").LineNum;
            }
            else
            {
                lineNumber = CurrentToken.LineNum;
            }

            List<Node> statements = new List<Node>();

            while (!Check(TokenType.Dedent) && !Check(TokenType.EndOfCode))
            {
                statements.Add(ParseStatement());

                if (Check(TokenType.Newline))
                {
                    MoveToNextToken();
                }
                else if (!Check(TokenType.Dedent) && !Check(TokenType.EndOfCode))
                {
                    throw new ParserException("Expected newline after statement.", CurrentToken.LineNum);
                }
            }

            if (!isTopLevel)
            {
                Consume(TokenType.Dedent, "Expected dedent.");
            }

            return new BlockNode(statements, lineNumber);
        }

        #endregion

        #region Statement Methods

        Node ParseStatement()
        {
            if (Check(TokenType.Identifier) && CheckNext(TokenType.Equal))
            {
                return ParseVariableAssignment();
            }

            switch (CurrentToken.Type)
            {
                case TokenType.Identifier:
                    return ParseVariableAssignment();
                default:
                    throw new ParserException("Expected statement.", CurrentToken.LineNum);
            }
        }

        Node ParseVariableAssignment()
        {
            Token identifierToken = Consume(TokenType.Identifier, "Expected variable name.");
            Consume(TokenType.Equal, "Expected '=' after variable name.");
            Node expression = ParseExpression();

            return new VariableAssignmentNode(identifierToken.Lexeme, expression, identifierToken.LineNum);
        }

        #endregion

        #region Expression Methods

        Node ParseExpression()
        {
            Node left = ParseTerm();

            while (IsTokenTypeMatch(TokenType.Plus, TokenType.Minus))
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

            while (IsTokenTypeMatch(TokenType.Star, TokenType.Slash, TokenType.SlashSlash, TokenType.Percent))
            {
                Token opToken = _tokens[_tokenIndex - 1];
                Node right = ParseFactor();
                left = new ExpressionNode(MapBinary(opToken.Type), left, right, opToken.LineNum);
            }

            return left;
        }

        Node ParseFactor()
        {
            if (IsTokenTypeMatch(TokenType.Minus))
            {
                Token opToken = _tokens[_tokenIndex - 1];
                return new ExpressionNode(ExpressionOperator.Negate, ParseFactor(), opToken.LineNum);
            }

            return ParseExponent();
        }

        Node ParseExponent()
        {
            Node left = ParsePrimary();

            if (IsTokenTypeMatch(TokenType.StarStar))
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
                case TokenType.Identifier:
                    Token identifierToken = CurrentToken;
                    MoveToNextToken();
                    return new VariableFactorNode(identifierToken.Lexeme, identifierToken.LineNum);

                case TokenType.Integer:
                case TokenType.Float:
                    Token numericToken = CurrentToken;
                    MoveToNextToken();
                    return new LiteralNode(numericToken.Literal, numericToken.LineNum);

                case TokenType.LeftParenthesis:
                    MoveToNextToken();
                    Node inner = ParseExpression();
                    Consume(TokenType.RightParenthesis, "Expected ')' after expression.");
                    return inner;

                default:
                    throw new ParserException("Expected expression.", CurrentToken.LineNum);
            }
        }

        #endregion

        #region Token Helpers

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

        bool CheckNext(TokenType type)
        {
            int nextIndex = _tokenIndex + 1;
            return nextIndex < _tokens.Count && _tokens[nextIndex].Type == type;
        }

        bool IsTokenTypeMatch(params TokenType[] types)
        {
            foreach (TokenType type in types)
            {
                if (!Check(type))
                {
                    continue;
                }

                MoveToNextToken();
                return true;
            }

            return false;
        }

        Token Consume(TokenType type, string message)
        {
            if (!Check(type))
            {
                throw new ParserException(message, CurrentToken.LineNum);
            }

            Token token = CurrentToken;
            MoveToNextToken();
            return token;
        }

        #endregion

        #region Helper Methods

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

        #endregion
    }
}
