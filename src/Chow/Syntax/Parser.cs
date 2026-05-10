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
        List<Token> _tkns;
        int _tknIdx;

        private Token CurrTkn => _tkns[_tknIdx];
        private Token PrevTkn => _tkns[_tknIdx - 1];

        public Parser(List<Token> tkns)
        {
            _tkns = tkns;
        }

        #region Primary Methods

        public Node BuildTree()
        {
            if (_tkns.Count == 0)
            {
                return new EmptyNode();
            }

            Node module = ParseModule();
            ConsumeCurrTkn(TokenType.EndOfCode, "Expected end of code.");
            return new RootNode(module, module.LineNum);
        }

        Node ParseModule()
        {
            List<Node> stmnts = new List<Node>();

            // Even when modules contain no statements, they are still valid, seeing as their top-level code
            while (!CurrTknType(TokenType.EndOfCode))
            {
                // The only valid lines start with a newline or the start of a statement
                if (CurrTknType(TokenType.Newline))
                {
                    MoveNextTkn();
                    continue;
                }

                // This will throw an exception if the current token is not the start of a statement
                stmnts.Add(ParseStmnts());
            }

            return new ModuleNode(stmnts);
        }

        #endregion

        #region Statement Methods

        Node ParseStmnts()
        {
            switch (CurrTkn.type)
            {
                case TokenType.Identifier:
                    if (NextTknType(TokenType.SymbolAssign))
                    {
                        return ParseVarAssignment();
                    }

                    // If no assignment operator is after the, then assume the identifier is a primary in an expression
                    break;

                case TokenType.KeywordReturn:
                    return ParseReturn();
            }

            if (IsCurrPrimaryTkn())
            {
                // These standalone expressions and their result will be discarded OR be sent to a special execution hook
                return ParseExprStmnt();
            }

            throw new ParserEx("Expected statement.", CurrTkn.lineNum);
        }

        Node ParseVarAssignment()
        {
            Token identifierToken = ConsumeCurrTkn(TokenType.Identifier, "Expected variable name.");
            ConsumeCurrTkn(TokenType.SymbolAssign, "Expected '=' after variable name.");
            Node expression = ParseExpr();
            return new VariableAssignNode(identifierToken.lexeme, expression, identifierToken.lineNum);
        }

        Node ParseReturn()
        {
            int lineNumber = CurrTkn.lineNum;
            ConsumeCurrTkn(TokenType.KeywordReturn, "Expected 'return' keyword.");
            Node expression;

            if (IsCurrPrimaryTkn())
            {
                expression = ParseExpr();
            }
            else
            {
                // Void functions always return None, and their calls inside expressions will not cause an error
                expression = null;
            }

            return new ReturnNode(expression, lineNumber);
        }

        Node ParseExprStmnt()
        {
            int lineNum = CurrTkn.lineNum;
            Node expression = ParseExpr();
            return new ExprStatementNode(expression, lineNum);
        }

        #endregion

        #region Expression Methods

        Node ParseExpr()
        {
            return ParseOr();
        }

        Node ParseOr()
        {
            Node l = ParseAnd();

            while (IsCurrTknType(TokenType.KeywordOr))
            {
                Token opTkn = PrevTkn;
                Node r = ParseAnd();
                l = new ExprNode(ExprOperator.Or, l, r, opTkn.lineNum);
            }

            return l;
        }

        Node ParseAnd()
        {
            Node l = ParseNot();

            while (IsCurrTknType(TokenType.KeywordAnd))
            {
                Token opTkn = PrevTkn;
                Node r = ParseNot();
                l = new ExprNode(ExprOperator.And, l, r, opTkn.lineNum);
            }

            return l;
        }

        Node ParseNot()
        {
            if (IsCurrTknType(TokenType.KeywordNot))
            {
                Token opTkn = PrevTkn;
                return new ExprNode(ExprOperator.Not, ParseNot(), opTkn.lineNum);
            }

            return ParseComparison();
        }

        Node ParseComparison()
        {
            Node l = ParseAdd();
            Node result = null;

            while (IsCurrTknType(
                TokenType.SymbolEqualTo,
                TokenType.SymbolNotEqual,
                TokenType.SymbolLess,
                TokenType.SymbolGreater,
                TokenType.SymbolLessEqual,
                TokenType.SymbolGreaterEqual))
            {
                Token opTkn = PrevTkn;
                Node r = ParseAdd();
                Node comparison = new ExprNode(MapBinary(opTkn.type), l, r, opTkn.lineNum);

                if (result == null)
                {
                    result = comparison;
                }
                else
                {
                    result = new ExprNode(ExprOperator.And, result, comparison, opTkn.lineNum);
                }

                l = r;
            }

            return result ?? l;
        }

        Node ParseAdd()
        {
            Node l = ParseTerm();

            while (IsCurrTknType(TokenType.SymbolPlus, TokenType.SymbolMinus))
            {
                Token opTkn = PrevTkn;
                Node r = ParseTerm();
                l = new ExprNode(MapBinary(opTkn.type), l, r, opTkn.lineNum);
            }

            return l;
        }

        Node ParseTerm()
        {
            Node l = ParseFactor();

            while (IsCurrTknType(TokenType.SymbolMultiply, TokenType.SymbolDivide, TokenType.SymbolFloorDivide, TokenType.SymbolPercent))
            {
                Token opTkn = PrevTkn;
                Node r = ParseFactor();
                l = new ExprNode(MapBinary(opTkn.type), l, r, opTkn.lineNum);
            }

            return l;
        }

        Node ParseFactor()
        {
            if (IsCurrTknType(TokenType.SymbolMinus))
            {
                Token opTkn = PrevTkn;
                return new ExprNode(ExprOperator.Negate, ParseFactor(), opTkn.lineNum);
            }

            return ParseExponent();
        }

        Node ParseExponent()
        {
            Node l = ParsePrimary();

            if (IsCurrTknType(TokenType.SymbolExponent))
            {
                Token opTkn = PrevTkn;
                Node r = ParseFactor();
                return new ExprNode(ExprOperator.Exponentiate, l, r, opTkn.lineNum);
            }

            return l;
        }

        Node ParsePrimary()
        {
            // Note: After adding a new primary token type, remember to update IsPrimaryTokenType() as well. Not doing
            //       so will cause IsPrimaryTokenType() to return false for the new TokenType, which will break certain
            //       statements that behaviors rely on knowing whether an expression is present or not (e.g. return statements).
            switch (CurrTkn.type)
            {
                case TokenType.Identifier:
                    Token idTkn = CurrTkn;
                    MoveNextTkn();
                    return new NameNode(idTkn.lexeme, idTkn.lineNum);

                case TokenType.LiteralInt:
                case TokenType.LiteralFloat:
                    Token numTkn = CurrTkn;
                    MoveNextTkn();
                    return new LiteralNode(numTkn.literal, numTkn.lineNum);

                case TokenType.KeywordNone:
                    ConsumeCurrTkn(TokenType.KeywordNone);
                    return new LiteralNode(null, PrevTkn.lineNum);

                case TokenType.KeywordTrue:
                    ConsumeCurrTkn(TokenType.KeywordTrue);
                    return new LiteralNode(true, PrevTkn.lineNum);

                case TokenType.KeywordFalse:
                    ConsumeCurrTkn(TokenType.KeywordFalse);
                    return new LiteralNode(false, PrevTkn.lineNum);

                case TokenType.SymbolLeftParen:
                    MoveNextTkn();
                    Node inner = ParseExpr();
                    ConsumeCurrTkn(TokenType.SymbolRightParen);
                    return inner;

                default:
                    throw new ParserEx("Expected expression.", CurrTkn.lineNum);
            }
        }

        #endregion

        #region Token Helpers

        void MoveNextTkn()
        {
            if (CurrTkn.type != TokenType.EndOfCode)
            {
                _tknIdx++;
            }
        }

        bool CurrTknType(TokenType type)
        {
            return CurrTkn.type == type;
        }

        bool NextTknType(TokenType type)
        {
            int nextIndex = _tknIdx + 1;

            // This method will never be called when the current token is EndOfCode, so we don't need to check for out-of-range.
            return _tkns[nextIndex].type == type;
        }

        bool IsCurrTknType(params TokenType[] types)
        {
            foreach (TokenType type in types)
            {
                if (!CurrTknType(type))
                {
                    continue;
                }

                MoveNextTkn();
                return true;
            }

            return false;
        }

        Token ConsumeCurrTkn(TokenType type, string message = "")
        {
            if (!CurrTknType(type))
            {
                throw new ParserEx(message, CurrTkn.lineNum);
            }

            Token token = CurrTkn;
            MoveNextTkn();
            return token;
        }

        #endregion

        #region Helper Methods

        bool IsCurrPrimaryTkn()
        {
            TokenType type = CurrTkn.type;
            return type == TokenType.Identifier ||
                   type == TokenType.LiteralInt ||
                   type == TokenType.LiteralFloat ||
                   type == TokenType.KeywordNone ||
                   type == TokenType.KeywordTrue ||
                   type == TokenType.KeywordFalse ||
                   type == TokenType.KeywordNot ||
                   type == TokenType.SymbolLeftParen;
        }

        static ExprOperator MapBinary(TokenType type)
        {
            switch (type)
            {
                case TokenType.SymbolPlus:
                    return ExprOperator.Add;

                case TokenType.SymbolMinus:
                    return ExprOperator.Subtract;

                case TokenType.SymbolMultiply:
                    return ExprOperator.Multiply;

                case TokenType.SymbolDivide:
                    return ExprOperator.Divide;

                case TokenType.SymbolPercent:
                    return ExprOperator.Modulus;

                case TokenType.SymbolExponent:
                    return ExprOperator.Exponentiate;

                case TokenType.SymbolFloorDivide:
                    return ExprOperator.FloorDivide;

                case TokenType.SymbolEqualTo:
                    return ExprOperator.Equal;

                case TokenType.SymbolNotEqual:
                    return ExprOperator.NotEqual;

                case TokenType.SymbolLess:
                    return ExprOperator.Less;

                case TokenType.SymbolGreater:
                    return ExprOperator.Greater;

                case TokenType.SymbolLessEqual:
                    return ExprOperator.LessEqual;

                case TokenType.SymbolGreaterEqual:
                    return ExprOperator.GreaterEqual;

                case TokenType.KeywordAnd:
                    return ExprOperator.And;

                case TokenType.KeywordOr:
                    return ExprOperator.Or;

                default:
                    throw new InvalidOperationException();
            }
        }

        #endregion
    }
}
