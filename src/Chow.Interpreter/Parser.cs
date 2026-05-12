using Chow.Interpreter.Exceptions;
using Chow.Interpreter.Tokens;
using System;
using System.Collections.Generic;
using Chow.Interpreter.SyntaxTrees;
using Chow.Interpreter.SyntaxTrees.Expressions;
using Chow.Interpreter.SyntaxTrees.Statements;

namespace Chow.Interpreter
{
    class Parser
    {
        readonly List<Token> _tkns;
        int _tknIdx;

        Token CurrToken => _tkns[_tknIdx];
        Token PrevTkn => _tkns[_tknIdx - 1];

        public Parser(List<Token> tkns)
        {
            _tkns = tkns;
        }

        #region Primary Methods

        public Node BuildTree()
        {
            var stmnts = new List<Node>();
            var isComplete = IsTokenType(TokenType.EndOfCode);

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
                var stmnt = ParseStmnt();
                stmnts.Add(stmnt);

                isComplete = IsTokenType(TokenType.EndOfCode);

                // The last statement does not need a newline. Block statements (def/if) end with a
                // Dedent that already terminates them, so a trailing Newline after them is optional.
                if (isComplete)
                {
                    continue;
                }

                var isStmntWithBlock = stmnt is FunctionNode || stmnt is IfNode || stmnt is WhileNode;

                if (isStmntWithBlock)
                {
                    TryConsumeType(TokenType.Newline);
                    continue;
                }

                ConsumeToken(TokenType.Newline, "Expected newline after statement.");
            }

            ConsumeToken(TokenType.EndOfCode, "Expected end of code.");
            return new TreeRootNode(stmnts);
        }
        Node ParseBlock()
        {
            ConsumeToken(TokenType.SymbolColon, "Expected ':' after block header.");
            ConsumeToken(TokenType.Newline, "Expected newline after ':'.");

            var indentTkn = ConsumeToken(TokenType.Indent, "Expected indented block body.");
            var stmnts = new List<Node>
            {
                ParseStmnt()
            };

            TryConsumeType(TokenType.Newline);

            while (!IsTokenType(TokenType.Dedent))
            {
                if (IsTokenType(TokenType.Newline))
                {
                    ConsumeToken();
                }
                else
                {
                    stmnts.Add(ParseStmnt());
                    TryConsumeType(TokenType.Newline);
                }
            }

            ConsumeToken(TokenType.Dedent, "Expected dedent to close block.");
            return new BlockNode(stmnts, indentTkn.lineNum);
        }

        #endregion

        #region Statement Methods

        Node ParseStmnt()
        {
            switch (CurrToken.type)
            {
                case TokenType.KeywordReturn:
                    return ParseReturn();

                case TokenType.KeywordIf:
                    return ParseIf();

                case TokenType.KeywordDef:
                    return ParseFunction();

                case TokenType.KeywordWhile:
                    return ParseWhile();

                case TokenType.KeywordBreak:
                    return ParseBreak();

                case TokenType.KeywordContinue:
                    return ParseContinue();
            }

            if (!IsPrimaryToken())
            {
                throw new ParserEx("Expected statement.", CurrToken.lineNum);
            }

            // Parse expression first; if an '=' follows, convert the LHS into the appropriate assignment node.
            // Otherwise this is a standalone expression statement (result discarded or routed to hook).
            var startLine = CurrToken.lineNum;
            var lhs = ParseExpr();

            if (!TryConsumeType(TokenType.SymbolAssign))
            {
                return new ExprStatementNode(lhs, startLine);
            }

            var eqLine = PrevTkn.lineNum;
            var rhs = ParseExpr();
            return MakeAssignFromTarget(lhs, rhs, eqLine);
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
            var line = CurrToken.lineNum;

            ConsumeToken(TokenType.KeywordDef, "Expected 'def' keyword.");

            var nameTkn = ConsumeToken(TokenType.Identifier, "Expected function name.");

            ConsumeToken(TokenType.SymbolLeftParen, "Expected '(' after function name.");

            var paramList = new List<Node>();

            if (!IsTokenType(TokenType.SymbolRightParen))
            {
                var paramTkn = ConsumeToken(TokenType.Identifier, "Expected parameter name.");
                paramList.Add(new NameNode(paramTkn.lexeme, paramTkn.lineNum));

                while (TryConsumeType(TokenType.SymbolComma))
                {
                    paramTkn = ConsumeToken(TokenType.Identifier, "Expected parameter name after ','.");
                    paramList.Add(new NameNode(paramTkn.lexeme, paramTkn.lineNum));
                }
            }

            ConsumeToken(TokenType.SymbolRightParen, "Expected ')' after parameter list.");
            var body = ParseBlock();

            return new FunctionNode(nameTkn.lexeme, paramList, body, line);
        }

        Node ParseIf()
        {
            var line = CurrToken.lineNum;
            ConsumeToken(TokenType.KeywordIf, "Expected 'if' keyword.");

            var expr = ParseExpr();
            var block = ParseBlock();
            var branch = ParseBranch();

            return new IfNode(expr, block, branch, line);
        }

        Node ParseBranch()
        {
            if (IsTokenType(TokenType.KeywordElif))
            {
                var line = CurrToken.lineNum;
                ConsumeToken();

                var expr = ParseExpr();
                var block = ParseBlock();
                var branch = ParseBranch();

                return new BranchStmntNode(expr, block, branch, line);
            }

            if (IsTokenType(TokenType.KeywordElse))
            {
                var line = CurrToken.lineNum;
                ConsumeToken();

                var block = ParseBlock();
                return new BranchStmntNode(null, block, null, line);
            }

            return null;
        }

        Node ParseWhile()
        {
            var line = CurrToken.lineNum;
            ConsumeToken(TokenType.KeywordWhile, "Expected 'while' keyword.");

            var expr = ParseExpr();
            var block = ParseBlock();

            return new WhileNode(expr, block, line);
        }

        Node ParseBreak()
        {
            var line = CurrToken.lineNum;
            ConsumeToken(TokenType.KeywordBreak, "Expected 'break' keyword.");
            return new BreakNode(line);
        }

        Node ParseContinue()
        {
            var line = CurrToken.lineNum;
            ConsumeToken(TokenType.KeywordContinue, "Expected 'continue' keyword.");
            return new ContinueNode(line);
        }

        Node ParseReturn()
        {
            var line = CurrToken.lineNum;
            ConsumeToken(TokenType.KeywordReturn, "Expected 'return' keyword.");

            // Void functions always return None, and their calls inside expressions will not cause an error
            var expr = IsPrimaryToken() ? ParseExpr() : null;

            return new ReturnNode(expr, line);
        }

        #endregion

        #region Expression Methods

        Node ParseExpr()
        {
            return ParseOr();
        }

        Node ParseOr()
        {
            var l = ParseAnd();

            while (TryConsumeType(TokenType.KeywordOr))
            {
                var opTkn = PrevTkn;
                var r = ParseAnd();
                l = new ExprNode(ExprOperator.Or, l, r, opTkn.lineNum);
            }

            return l;
        }

        Node ParseAnd()
        {
            var l = ParseNot();

            while (TryConsumeType(TokenType.KeywordAnd))
            {
                var opTkn = PrevTkn;
                var r = ParseNot();
                l = new ExprNode(ExprOperator.And, l, r, opTkn.lineNum);
            }

            return l;
        }

        Node ParseNot()
        {
            if (TryConsumeType(TokenType.KeywordNot))
            {
                var opTkn = PrevTkn;
                return new ExprNode(ExprOperator.Not, ParseNot(), opTkn.lineNum);
            }

            return ParseComparison();
        }

        Node ParseComparison()
        {
            var l = ParseBitOr();
            Node result = null;

            while (true)
            {
                ExprOperator op;
                int opLine;

                if (TryConsumeType(
                    TokenType.SymbolEqualTo,
                    TokenType.SymbolNotEqual,
                    TokenType.SymbolLess,
                    TokenType.SymbolGreater,
                    TokenType.SymbolLessEqual,
                    TokenType.SymbolGreaterEqual,
                    TokenType.KeywordIn))
                {
                    op = MapBinary(PrevTkn.type);
                    opLine = PrevTkn.lineNum;
                }
                else if (IsTokenType(TokenType.KeywordNot) && PeekTokenType(TokenType.KeywordIn))
                {
                    opLine = CurrToken.lineNum;
                    ConsumeToken();
                    ConsumeToken();
                    op = ExprOperator.NotIn;
                }
                else
                {
                    break;
                }

                var r = ParseBitOr();
                Node comparison = new ExprNode(op, l, r, opLine);

                if (result == null)
                {
                    result = comparison;
                }
                else
                {
                    result = new ExprNode(ExprOperator.And, result, comparison, opLine);
                }

                l = r;
            }

            return result ?? l;
        }

        Node ParseBitOr()
        {
            var l = ParseAdd();

            while (TryConsumeType(TokenType.SymbolPipe))
            {
                var opTkn = PrevTkn;
                var r = ParseAdd();
                l = new ExprNode(ExprOperator.BinaryOr, l, r, opTkn.lineNum);
            }

            return l;
        }

        Node ParseAdd()
        {
            var l = ParseTerm();

            while (TryConsumeType(TokenType.SymbolPlus, TokenType.SymbolMinus))
            {
                var opTkn = PrevTkn;
                var r = ParseTerm();
                l = new ExprNode(MapBinary(opTkn.type), l, r, opTkn.lineNum);
            }

            return l;
        }

        Node ParseTerm()
        {
            var l = ParseFactor();

            while (TryConsumeType(TokenType.SymbolMultiply, TokenType.SymbolDivide, TokenType.SymbolFloorDivide, TokenType.SymbolPercent))
            {
                var opTkn = PrevTkn;
                var r = ParseFactor();
                l = new ExprNode(MapBinary(opTkn.type), l, r, opTkn.lineNum);
            }

            return l;
        }

        Node ParseFactor()
        {
            if (TryConsumeType(TokenType.SymbolMinus))
            {
                var opTkn = PrevTkn;
                return new ExprNode(ExprOperator.Negate, ParseFactor(), opTkn.lineNum);
            }

            return ParseExponent();
        }

        Node ParseExponent()
        {
            var l = ParsePostfix();

            if (TryConsumeType(TokenType.SymbolExponent))
            {
                var opTkn = PrevTkn;
                var r = ParseFactor();
                return new ExprNode(ExprOperator.Exponentiate, l, r, opTkn.lineNum);
            }

            return l;
        }

        Node ParsePostfix()
        {
            // Accounts for dot notation, indexers, and function calls (e.g. parenthesis, arguments, etc.)
            var node = ParsePrimary();
            var isDone = false;

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
            var dotTkn = ConsumeToken(TokenType.SymbolDot, "Expected '.'.");
            var nameTkn = ConsumeToken(TokenType.Identifier, "Expected attribute name after '.'.");

            return new AttrAccessNode(targ, nameTkn.lexeme, dotTkn.lineNum);
        }

        Node ParseSubscriptTail(Node targ)
        {
            var leftBr = ConsumeToken(TokenType.SymbolLeftBracket, "Expected '['.");
            var idx = ParseSubscriptBody();

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
                var first = ParseExpr();

                if (!IsTokenType(TokenType.SymbolColon))
                {
                    // Plain index a[i]
                    return first;
                }

                start = first;
            }

            // Positioned on first ':'
            var sliceLine = CurrToken.lineNum;
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
            var leftParen = ConsumeToken(TokenType.SymbolLeftParen, "Expected '('.");
            var args = new List<Node>();

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
            var leftBr = ConsumeToken(TokenType.SymbolLeftBracket, "Expected '['.");
            var elems = new List<Node>();

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

        Node ParseDictLiteral()
        {
            var leftCurly = ConsumeToken(TokenType.SymbolLeftCurly, "Expected '{'.");
            var keys = new List<Node>();
            var values = new List<Node>();

            if (!IsTokenType(TokenType.SymbolRightCurly))
            {
                ParseDictEntry(keys, values);

                while (TryConsumeType(TokenType.SymbolComma) && !IsTokenType(TokenType.SymbolRightCurly))
                {
                    ParseDictEntry(keys, values);
                }
            }

            ConsumeToken(TokenType.SymbolRightCurly, "Expected '}' to close dict literal.");
            return new DictLiteralNode(keys, values, leftCurly.lineNum);
        }

        void ParseDictEntry(List<Node> keys, List<Node> values)
        {
            var key = ParseExpr();
            ConsumeToken(TokenType.SymbolColon, "Expected ':' between dict key and value.");
            var value = ParseExpr();
            keys.Add(key);
            values.Add(value);
        }

        Node ParsePrimary()
        {
            // Note: After adding a new primary token type, remember to update IsPrimaryTokenType() as well. Not doing
            //       so will cause IsPrimaryTokenType() to return false for the new TokenType, which will break certain
            //       statements that behaviors rely on knowing whether an expression is present or not (e.g. return statements).
            switch (CurrToken.type)
            {
                case TokenType.Identifier:
                    var idTkn = CurrToken;
                    ConsumeToken();
                    return new NameNode(idTkn.lexeme, idTkn.lineNum);

                case TokenType.LiteralInt:
                case TokenType.LiteralFloat:
                case TokenType.LiteralStr:
                    var numTkn = CurrToken;
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
                    var inner = ParseExpr();
                    ConsumeToken(TokenType.SymbolRightParen);
                    return inner;

                case TokenType.SymbolLeftBracket:
                    return ParseListLiteral();

                case TokenType.SymbolLeftCurly:
                    return ParseDictLiteral();

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
            var nextIndex = _tknIdx + offset;

            // This method will never be called when the current token is EndOfCode, so we don't need to check for out-of-range.
            return _tkns[nextIndex].type == type;
        }

        bool TryConsumeType(params TokenType[] types)
        {
            foreach (var type in types)
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

            var token = CurrToken;
            ConsumeToken();
            return token;
        }

        bool IsPrimaryToken()
        {
            var type = CurrToken.type;
            return type == TokenType.Identifier ||
                   type == TokenType.LiteralInt ||
                   type == TokenType.LiteralFloat ||
                   type == TokenType.LiteralStr ||
                   type == TokenType.KeywordNone ||
                   type == TokenType.KeywordTrue ||
                   type == TokenType.KeywordFalse ||
                   type == TokenType.KeywordNot ||
                   type == TokenType.SymbolLeftParen ||
                   type == TokenType.SymbolLeftBracket ||
                   type == TokenType.SymbolLeftCurly;
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

                case TokenType.SymbolPipe:
                    return ExprOperator.BinaryOr;

                case TokenType.KeywordIn:
                    return ExprOperator.In;

                default:
                    throw new InvalidOperationException();
            }
        }

        #endregion
    }
}
