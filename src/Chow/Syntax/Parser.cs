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

            Node block = ParseBlock(isTopLevel: true);
            Consume(TokenType.EndOfCode, "Expected end of code.");
            return new SyntaxTreeRoot(block, block.LineNumber);
        }

        // TODO: Split top-level parsing and nested block parsing
        Node ParseBlock(bool isTopLevel = false)
        {
            int lineNumber;

            if (!isTopLevel)
            {
                // Function definitions and conditional statements (before their bodies) will include a colon
                Consume(TokenType.Colon, "Expected colon before block.");

                // This case does not account for the indent level (there currently is only top-level code)
                lineNumber = Consume(TokenType.Indent, "Expected indent.").LineNum;
            }
            else
            {
                lineNumber = CurrentToken.LineNum;
            }

            List<Node> statements = new List<Node>();
            
            // At least one statement is expected to be a valid block
            bool isStatementNext = true;

            // Each iteration will start at the beginning of a line
            while (isStatementNext)
            {
                // Skip any blank lines between statements or at the end of blocks.
                if (Check(TokenType.Newline))
                {
                    MoveToNextToken();
                    continue;
                }
                // A dedent signifies a statement outside this block, meaning this block has ended (if not top-level).
                else if (Check(TokenType.Dedent) && !isTopLevel)
                {
                    // Don't consume as the dedent will be consumed after this loop
                    isStatementNext = false;
                    continue;
                }

                statements.Add(ParseStatement());

                // Statements must be seperated by newlines, so if not at the end of the code expect a newline.
                if (Check(TokenType.Newline))
                {
                    MoveToNextToken();
                }
                else if (Check(TokenType.EndOfCode))
                {
                    isStatementNext = false;
                    continue;
                }
                else
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

                case TokenType.Return:
                    return ParseReturn();

                default:
                    throw new ParserException("Expected statement.", CurrentToken.LineNum);
            }
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

            return new ReturnNode(expression, CurrentToken.LineNum);
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
            // Note: After adding a new primary token type, remember to update IsPrimaryTokenType() as well. Not doing
            //       so will cause IsPrimaryTokenType() to return false for the new TokenType, which will break certain
            //       statements that behaviors rely on knowing whether an expression is present or not (e.g. return statements).
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
