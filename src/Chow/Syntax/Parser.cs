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

        private Token CurrToken => _tkns[_tknIdx];
        private Token PrevTkn => _tkns[_tknIdx - 1];

        public Parser(List<Token> tkns)
        {
            _tkns = tkns;
        }

        #region Primary Methods

        public Node BuildTree()
        {
            List<Node> stmnts = new List<Node>();
            bool isComplete = IsTokenType(TokenType.EndOfCode);

            // Even code contains no statements, it is still vali
            while (!isComplete)
            {
                // The only valid lines start with a newline or the start of a statement
                if (IsTokenType(TokenType.Newline))
                {
                    ConsumeToken();
                    isComplete = IsTokenType(TokenType.EndOfCode);
                    continue;
                }

                // This will throw an exception if the current token is not the start of a statement
                Node stmnt = ParseStmnts();
                stmnts.Add(stmnt);

                isComplete = IsTokenType(TokenType.EndOfCode);

                // The last statement does not need a newline. Block statements (def/if) end with a
                // Dedent that already terminates them, so a trailing Newline after them is optional.
                if (isComplete)
                {
                    continue;
                }

                bool isBlockStmnt = stmnt is FunctionNode || stmnt is IfNode;

                if (isBlockStmnt)
                {
                    TryConsumeType(TokenType.Newline);
                }
                else
                {
                    ConsumeToken(TokenType.Newline, "Expected newline after statement.");
                }
            }

            ConsumeToken(TokenType.EndOfCode, "Expected end of code.");
            return new TreeRootNode(stmnts);
        }
        Node ParseBlock()
        {
            ConsumeToken(TokenType.SymbolColon, "Expected ':' after block header.");
            ConsumeToken(TokenType.Newline, "Expected newline after ':'.");
            Token indentTkn = ConsumeToken(TokenType.Indent, "Expected indented block body.");

            List<Node> stmnts = new List<Node>();
            stmnts.Add(ParseStmnts());
            TryConsumeType(TokenType.Newline);

            while (!IsTokenType(TokenType.Dedent))
            {
                if (IsTokenType(TokenType.Newline))
                {
                    ConsumeToken();
                    continue;
                }

                stmnts.Add(ParseStmnts());
                TryConsumeType(TokenType.Newline);
            }

            ConsumeToken(TokenType.Dedent, "Expected dedent to close block.");
            return new BlockNode(stmnts, indentTkn.lineNum);
        }

        #endregion

        #region Statement Methods

        Node ParseStmnts()
        {
            switch (CurrToken.type)
            {
                case TokenType.KeywordReturn:
                    return ParseReturn();

                case TokenType.KeywordIf:
                    return ParseIf();

                case TokenType.KeywordDef:
                    return ParseFunction();
            }

            if (IsPrimaryToken())
            {
                // Parse expression first; if an '=' follows, convert the LHS into the appropriate assignment node.
                // Otherwise this is a standalone expression statement (result discarded or routed to hook).
                int startLine = CurrToken.lineNum;
                Node lhs = ParseExpr();

                if (TryConsumeType(TokenType.SymbolAssign))
                {
                    int eqLine = PrevTkn.lineNum;
                    Node rhs = ParseExpr();
                    return MakeAssignFromTarget(lhs, rhs, eqLine);
                }

                return new ExprStatementNode(lhs, startLine);
            }

            throw new ParserEx("Expected statement.", CurrToken.lineNum);
        }

        Node MakeAssignFromTarget(Node target, Node value, int line)
        {
            switch (target)
            {
                case NameNode nameNode:
                    return new VarAssignNode(nameNode.Name, value, line);

                case SubscriptNode subscrNode:
                    return new SubscriptAssignNode(subscrNode.Target, subscrNode.Index, value, line);

                case AttrAccessNode attrNode:
                    return new AttrAssignNode(attrNode.Target, attrNode.AttrName, value, line);

                default:
                    throw new ParserEx("Invalid assignment target.", line);
            }
        }

        Node ParseFunction()
        {
            int lineNum = CurrToken.lineNum;

            ConsumeToken(TokenType.KeywordDef, "Expected 'def' keyword.");

            Token nameTkn = ConsumeToken(TokenType.Identifier, "Expected function name.");
            ConsumeToken(TokenType.SymbolLeftParen, "Expected '(' after function name.");

            List<Node> paramList = new List<Node>();

            if (!IsTokenType(TokenType.SymbolRightParen))
            {
                Token paramTkn = ConsumeToken(TokenType.Identifier, "Expected parameter name.");
                paramList.Add(new NameNode(paramTkn.lexeme, paramTkn.lineNum));

                while (TryConsumeType(TokenType.SymbolComma))
                {
                    paramTkn = ConsumeToken(TokenType.Identifier, "Expected parameter name after ','.");
                    paramList.Add(new NameNode(paramTkn.lexeme, paramTkn.lineNum));
                }
            }

            ConsumeToken(TokenType.SymbolRightParen, "Expected ')' after parameter list.");

            Node body = ParseBlock();
            return new FunctionNode(nameTkn.lexeme, paramList, body, lineNum);
        }

        Node ParseIf()
        {
            int lineNum = CurrToken.lineNum;
            ConsumeToken(TokenType.KeywordIf, "Expected 'if' keyword.");

            Node expr = ParseExpr();
            Node block = ParseBlock();
            Node branch = ParseBranch();
            
            return new IfNode(expr, block, branch, lineNum);
        }

        Node ParseBranch()
        {
            if (IsTokenType(TokenType.KeywordElif))
            {
                int lineNum = CurrToken.lineNum;
                ConsumeToken();
                
                Node expr = ParseExpr();
                Node block = ParseBlock();
                Node branch = ParseBranch();

                return new BranchStmntNode(expr, block, branch, lineNum);
            }

            if (IsTokenType(TokenType.KeywordElse))
            {
                int lineNum = CurrToken.lineNum;
                ConsumeToken();
                
                Node block = ParseBlock();
                return new BranchStmntNode(null, block, null, lineNum);
            }

            return null;
        }

        Node ParseReturn()
        {
            int lineNumber = CurrToken.lineNum;
            ConsumeToken(TokenType.KeywordReturn, "Expected 'return' keyword.");
            
            Node expression;

            if (IsPrimaryToken())
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
            int lineNum = CurrToken.lineNum;
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

            while (TryConsumeType(TokenType.KeywordOr))
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

            while (TryConsumeType(TokenType.KeywordAnd))
            {
                Token opTkn = PrevTkn;
                Node r = ParseNot();
                l = new ExprNode(ExprOperator.And, l, r, opTkn.lineNum);
            }

            return l;
        }

        Node ParseNot()
        {
            if (TryConsumeType(TokenType.KeywordNot))
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

            while (TryConsumeType(
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

            while (TryConsumeType(TokenType.SymbolPlus, TokenType.SymbolMinus))
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

            while (TryConsumeType(TokenType.SymbolMultiply, TokenType.SymbolDivide, TokenType.SymbolFloorDivide, TokenType.SymbolPercent))
            {
                Token opTkn = PrevTkn;
                Node r = ParseFactor();
                l = new ExprNode(MapBinary(opTkn.type), l, r, opTkn.lineNum);
            }

            return l;
        }

        Node ParseFactor()
        {
            if (TryConsumeType(TokenType.SymbolMinus))
            {
                Token opTkn = PrevTkn;
                return new ExprNode(ExprOperator.Negate, ParseFactor(), opTkn.lineNum);
            }

            return ParseExponent();
        }

        Node ParseExponent()
        {
            Node l = ParsePostfix();

            if (TryConsumeType(TokenType.SymbolExponent))
            {
                Token opTkn = PrevTkn;
                Node r = ParseFactor();
                return new ExprNode(ExprOperator.Exponentiate, l, r, opTkn.lineNum);
            }

            return l;
        }

        Node ParsePostfix()
        {
            // Accounts for dot notation, indexers, and function calls (e.g. parenthesis, arguments, etc.)
            Node node = ParsePrimary();
            bool isDone = false;

            do
            {
                switch (CurrToken.type)
                {
                    case TokenType.SymbolDot:
                        node = ParseAttrAccessTail(node);
                        break;
                    case TokenType.SymbolLeftBracket:
                        node = ParseSubscriptTail(node);
                        break;
                    case TokenType.SymbolLeftParen:
                        node = ParseInvokeTail(node);
                        break;
                    default:
                        isDone = true;
                        break;
                }
            }
            while (!isDone);

            return node;
        }

        Node ParseAttrAccessTail(Node targ)
        {
            Token dotTkn = ConsumeToken(TokenType.SymbolDot, "Expected '.'.");
            Token nameTkn = ConsumeToken(TokenType.Identifier, "Expected attribute name after '.'.");

            return new AttrAccessNode(targ, nameTkn.lexeme, dotTkn.lineNum);
        }

        Node ParseSubscriptTail(Node targ)
        {
            Token leftBr = ConsumeToken(TokenType.SymbolLeftBracket, "Expected '['.");
            Node idx = ParseSubscriptBody();

            ConsumeToken(TokenType.SymbolRightBracket, "Expected ']' to close subscript.");
            
            return new SubscriptNode(targ, idx, leftBr.lineNum);
        }

        Node ParseSubscriptBody()
        {
            Node start = null;
            Node stop = null;
            Node step = null;

            if (!IsTokenType(TokenType.SymbolColon))
            {
                Node first = ParseExpr();

                if (!IsTokenType(TokenType.SymbolColon))
                {
                    // Plain index a[i]
                    return first;
                }

                start = first;
            }

            // Positioned on first ':'
            int sliceLine = CurrToken.lineNum;
            ConsumeToken();

            if (!IsTokenType(TokenType.SymbolColon) && !IsTokenType(TokenType.SymbolRightBracket))
            {
                stop = ParseExpr();
            }

            if (TryConsumeType(TokenType.SymbolColon) && !IsTokenType(TokenType.SymbolRightBracket))
            {
                step = ParseExpr();
            }

            return new SliceNode(start, stop, step, sliceLine);
        }

        Node ParseInvokeTail(Node callNameNode)
        {
            Token leftParen = ConsumeToken(TokenType.SymbolLeftParen, "Expected '('.");
            List<Node> args = new List<Node>();

            if (!IsTokenType(TokenType.SymbolRightParen))
            {
                args.Add(ParseExpr());

                while (TryConsumeType(TokenType.SymbolComma))
                {
                    args.Add(ParseExpr());
                }
            }

            ConsumeToken(TokenType.SymbolRightParen, "Expected ')' after argument list.");
            return new CallNode(callNameNode, args, leftParen.lineNum);
        }

        Node ParseListLiteral()
        {
            Token leftBr = ConsumeToken(TokenType.SymbolLeftBracket, "Expected '['.");
            List<Node> elems = new List<Node>();

            if (!IsTokenType(TokenType.SymbolRightBracket))
            {
                elems.Add(ParseExpr());

                // Allow trailing comma: `[1, 2,]`
                while (TryConsumeType(TokenType.SymbolComma) && !IsTokenType(TokenType.SymbolRightBracket))
                {
                    elems.Add(ParseExpr());
                }
            }

            ConsumeToken(TokenType.SymbolRightBracket, "Expected ']' to close list literal.");
            return new ListLiteralNode(elems, leftBr.lineNum);
        }

        Node ParsePrimary()
        {
            // Note: After adding a new primary token type, remember to update IsPrimaryTokenType() as well. Not doing
            //       so will cause IsPrimaryTokenType() to return false for the new TokenType, which will break certain
            //       statements that behaviors rely on knowing whether an expression is present or not (e.g. return statements).
            switch (CurrToken.type)
            {
                case TokenType.Identifier:
                    Token idTkn = CurrToken;
                    ConsumeToken();
                    return new NameNode(idTkn.lexeme, idTkn.lineNum);

                case TokenType.LiteralInt:
                case TokenType.LiteralFloat:
                case TokenType.LiteralStr:
                    Token numTkn = CurrToken;
                    ConsumeToken();
                    return new LiteralNode(numTkn.literal, numTkn.lineNum);

                case TokenType.KeywordNone:
                    ConsumeToken(TokenType.KeywordNone);
                    return new LiteralNode(value: null, PrevTkn.lineNum);

                case TokenType.KeywordTrue:
                    ConsumeToken(TokenType.KeywordTrue);
                    return new LiteralNode(value: true, PrevTkn.lineNum);

                case TokenType.KeywordFalse:
                    ConsumeToken(TokenType.KeywordFalse);
                    return new LiteralNode(value: false, PrevTkn.lineNum);

                case TokenType.SymbolLeftParen:
                    ConsumeToken();
                    Node inner = ParseExpr();
                    ConsumeToken(TokenType.SymbolRightParen);
                    return inner;

                case TokenType.SymbolLeftBracket:
                    return ParseListLiteral();

                default:
                    throw new ParserEx("Expected expression.", CurrToken.lineNum);
            }
        }

        #endregion

        #region Token Helpers

        void ConsumeToken()
        {
            if (CurrToken.type != TokenType.EndOfCode)
            {
                _tknIdx++;
            }
        }

        bool IsTokenType(TokenType type)
        {
            return CurrToken.type == type;
        }

        bool PeekTokenType(TokenType type, int offset = 1)
        {
            int nextIndex = _tknIdx + offset;

            // This method will never be called when the current token is EndOfCode, so we don't need to check for out-of-range.
            return _tkns[nextIndex].type == type;
        }

        bool TryConsumeType(params TokenType[] types)
        {
            foreach (TokenType type in types)
            {
                if (!IsTokenType(type))
                {
                    continue;
                }

                ConsumeToken();
                return true;
            }

            return false;
        }

        Token ConsumeToken(TokenType type, string message = "")
        {
            if (!IsTokenType(type))
            {
                throw new ParserEx(message, CurrToken.lineNum);
            }

            Token token = CurrToken;
            ConsumeToken();
            return token;
        }

        bool IsPrimaryToken()
        {
            TokenType type = CurrToken.type;
            return type == TokenType.Identifier ||
                   type == TokenType.LiteralInt ||
                   type == TokenType.LiteralFloat ||
                   type == TokenType.LiteralStr ||
                   type == TokenType.KeywordNone ||
                   type == TokenType.KeywordTrue ||
                   type == TokenType.KeywordFalse ||
                   type == TokenType.KeywordNot ||
                   type == TokenType.SymbolLeftParen ||
                   type == TokenType.SymbolLeftBracket;
        }

        #endregion

        #region Helper Methods



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
