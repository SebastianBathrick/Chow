using Chow.Interpreter.Syntax.Trees;
using Chow.Interpreter.Syntax.Trees.Expressions;
using Chow.Interpreter.Syntax.Trees.Statements;
using Chow.Interpreter.Tokens;
using System;
using System.Collections.Generic;

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

            Node module = ParseModule();
            Consume(TokenType.EndOfCode, "Expected end of code.");
            return new SyntaxTreeRoot(module, module.LineNumber);
        }

        Node ParseModule()
        {
            List<Node> statements = new List<Node>();

            // Even when modules contain no statements, they are still valid, seeing as their top-level code
            while (!Check(TokenType.EndOfCode))
            {
                // The only valid lines start with a newline or the start of a statement
                if (Check(TokenType.Newline))
                {
                    MoveToNextToken();
                    continue;
                }

                // This will throw an exception if the current token is not the start of a statement
                statements.Add(ParseStatement());
            }

            return new ModuleNode(statements);
        }

        #endregion

        #region Statement Methods

        Node ParseStatement()
        {
            switch (CurrentToken.Type)
            {
                case TokenType.Identifier:
                    return ParseVariableAssignment();

                case TokenType.Return:
                    return ParseReturn();
            }

            if (IsPrimaryTokenType())
            {
                // These standalone expressions and their result will be discarded OR be sent to a special execution hook
                return ParseExpressionStatement();
            }

            throw new ParserException("Expected statement.", CurrentToken.LineNum);
        }

        Node ParseVariableAssignment()
        {
            Token identifierToken = Consume(TokenType.Identifier, "Expected variable name.");
            Consume(TokenType.Equal, "Expected '=' after variable name.");
            Node expression = ParseExpression();
            return new VariableAssignNode(identifierToken.Lexeme, expression, identifierToken.LineNum);
        }

        Node ParseReturn()
        {
            int lineNumber = CurrentToken.LineNum;
            Consume(TokenType.Return, "Expected 'return' keyword.");
            Node expression;

            if (IsPrimaryTokenType())
            {
                expression = ParseExpression();
            }
            else
            {
                // Void functions always return None, and their calls inside expressions will not cause an error
                expression = null;
            }

            return new ReturnNode(expression, lineNumber);
        }

        Node ParseExpressionStatement()
        {
            int lineNum = CurrentToken.LineNum;
            Node expression = ParseExpression();
            return new ExprStatementNode(expression, lineNum);
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
                left = new ExprNode(MapBinary(opToken.Type), left, right, opToken.LineNum);
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
                left = new ExprNode(MapBinary(opToken.Type), left, right, opToken.LineNum);
            }

            return left;
        }

        Node ParseFactor()
        {
            if (IsTokenTypeMatch(TokenType.Minus))
            {
                Token opToken = _tokens[_tokenIndex - 1];
                return new ExprNode(ExpressionOperator.Negate, ParseFactor(), opToken.LineNum);
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
                return new ExprNode(ExpressionOperator.Exponentiate, left, right, opToken.LineNum);
            }

            return left;
        }

        Node ParsePrimary()
        {
            // Note: After adding a new primary token type, remember to update IsPrimaryTokenType() as well. Not doing
            //       so will cause IsPrimaryTokenType() to return false for the new TokenType, which will break certain
            //       statements that behaviors rely on knowing whether an expression is present or not (e.g. return statements).
            switch (CurrentToken.Type)
            {
                case TokenType.Identifier:
                    Token identifierToken = CurrentToken;
                    MoveToNextToken();
                    return new IdentifierNode(identifierToken.Lexeme, identifierToken.LineNum);

                case TokenType.Integer:
                case TokenType.Float:
                    Token numericToken = CurrentToken;
                    MoveToNextToken();
                    return new LiteralNode(numericToken.Literal, numericToken.LineNum);

                case TokenType.None:
                    Consume(TokenType.None);
                    return new LiteralNode(null, CurrentToken.LineNum);

                case TokenType.LeftParenthesis:
                    MoveToNextToken();
                    Node inner = ParseExpression();
                    Consume(TokenType.RightParenthesis);
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

            // This method will never be called when the current token is EndOfCode, so we don't need to check for out-of-range.
            return _tokens[nextIndex].Type == type;
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

        Token Consume(TokenType type, string message = "")
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
        bool IsPrimaryTokenType()
        {
            TokenType type = CurrentToken.Type;
            return type == TokenType.Identifier ||
                   type == TokenType.Integer ||
                   type == TokenType.Float ||
                   type == TokenType.None ||
                   type == TokenType.LeftParenthesis;
        }

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
