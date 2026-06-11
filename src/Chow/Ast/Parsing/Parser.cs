using System;
using System.Collections.Generic;
using Chow.Tokens;
using Chow.Tokens.Scanning;
using Chow.Utility;

namespace Chow.Ast.Parsing
{
    sealed class Parser
    {
        const int ModuleNodeLineNumber = 1;
        
        readonly List<Token> _tokens;
        int _tokenIdx;

        Token CurrentToken => _tokens[_tokenIdx];
        
        Token PreviousToken => _tokens[_tokenIdx - 1];

        #region Main Methods
    
        /// <summary>Initializes a new instance with the tokens it will analyze.</summary>
        /// <param name="tokens">The tokens used to build an AST upon calling
        /// <see cref="BuildAst"/>.</param>
        public Parser(List<Token> tokens)
        {
            _tokens = tokens;
        }
        
        public Node BuildAst()
        {
            // Elements represent top-level statements
            var statementList = new List<Node>();
            
            // Iterate until reaching the 'end of code' marker
            while(!IsType(TokenType.EndOfCode))
            {
                if (!TryNext(TokenType.Newline))
                {
                    statementList.Add(ParseStatement());
                }
            }

            // It's assumed the final token always an 'end of code' marker
            Next(TokenType.EndOfCode);
            var block = new BlockNode(statementList, ModuleNodeLineNumber);
            return new ModuleNode(block);
        }

        #endregion

        #region Statement Methods

        Node ParseBlock()
        {
            Next(TokenType.SymbolColon, "Expected ':' after block header.");
            Next(TokenType.Newline, "Expected newline after ':'.");

            var indentToken = Next(TokenType.Indent, "Expected indented block body.");
            
            var statements = new List<Node> { ParseStatement() };

            
            
            TryNext(TokenType.Newline);

            while (!IsType(TokenType.Dedent))
            {
                if (IsType(TokenType.Newline))
                {
                    Next();
                    continue;
                }

                statements.Add(ParseStatement());
                TryNext(TokenType.Newline);
            }

            Next(TokenType.Dedent, "Expected dedent to close block.");
            return new BlockNode(statements, indentToken.LineNumber);
        }

        Node ParseStatement()
        {
            switch (CurrentToken.Type)
            {
                case TokenType.KeywordReturn:
                    return ParseReturnStatement();
                case TokenType.KeywordIf:
                    return ParseIfStatement();
                case TokenType.KeywordDef:
                    return ParseFunctionDefinition();
                case TokenType.KeywordWhile:
                    return ParseWhileStatement();
                case TokenType.KeywordFor:
                    return ParseForStatement();
                case TokenType.KeywordBreak:
                    return ParseBreakStatement();
                case TokenType.KeywordContinue:
                    return ParseContinueStatement();
                case TokenType.KeywordGlobal:
                    return ParseGlobalDeclaration();
                case TokenType.KeywordNonlocal:
                    return ParseNonlocalDeclaration();
            }

            if (!IsPrimaryToken())
            {
                throw new SyntaxException("Expected statement.", CurrentToken.LineNumber);
            }

            // Parse expression first; if an '=' follows, convert the LHS into the appropriate
            // assignment node. Otherwise, this is a standalone expression statement (result
            // discarded or routed to hook).
            var startLine = CurrentToken.LineNumber;
            var lhs = ParseExpression();

            if (!TryNext(TokenType.SymbolAssign))
            {
                return new ExpressionStatementNode(lhs, startLine);
            }

            var eqLine = PreviousToken.LineNumber;
            var rhs = ParseExpression();
            return MakeAssignFromTarget(lhs, rhs, eqLine);
        }

        Node ParseFunctionDefinition()
        {
            var line = CurrentToken.LineNumber;

            Next(TokenType.KeywordDef, "Expected 'def' keyword.");

            var nameToken = Next( TokenType.Identifier, "Expected function name.");

            Next(
                TokenType.SymbolLeftParen, "Expected '(' after function name.");

            var paramList = new List<Node>();

            if (!IsType(TokenType.SymbolRightParen))
            {
                var paramToken = Next( TokenType.Identifier, "Expected parameter name.");
                
                paramList.Add(new NameNode(paramToken.Lexeme, paramToken.LineNumber));

                while (TryNext(TokenType.SymbolComma))
                {
                    paramToken = Next(
                        TokenType.Identifier, "Expected parameter name after ','.");
                    
                    paramList.Add(new NameNode(paramToken.Lexeme, paramToken.LineNumber));
                }
            }

            Next(TokenType.SymbolRightParen, "Expected ')' after parameter list.");
            return new FunctionNode(nameToken.Lexeme, paramList, ParseBlock(), line);
        }

        Node ParseIfStatement()
        {
            var line = CurrentToken.LineNumber;
            Next(TokenType.KeywordIf, "Expected 'if' keyword.");
            return new IfStatementNode(
                ParseExpression(), ParseBlock(), ParseBranchStatement(), line);
        }

        Node ParseBranchStatement()
        {
            if (IsType(TokenType.KeywordElif))
            {
                var line = CurrentToken.LineNumber;
                Next();
                return new BranchStatementNode(
                    ParseExpression(), ParseBlock(), ParseBranchStatement(), line);
            }

            if (!IsType(TokenType.KeywordElse))
            {
                return null;
            }

            {
                var line = CurrentToken.LineNumber;
                Next();
                return new BranchStatementNode(null, ParseBlock(), null, line);
            }
        }

        Node ParseWhileStatement()
        {
            var line = CurrentToken.LineNumber;
            Next(TokenType.KeywordWhile, "Expected 'while' keyword.");
            return new WhileStatementNode(ParseExpression(), ParseBlock(), line);
        }

        Node ParseForStatement()
        {
            var line = CurrentToken.LineNumber;
            Next(TokenType.KeywordFor, "Expected 'for' keyword.");

            var targetToken = Next(
                TokenType.Identifier, "Expected loop variable name after 'for'.");
            var target = new NameNode(targetToken.Lexeme, targetToken.LineNumber);

            Next(TokenType.KeywordIn, "Expected 'in' after loop variable.");

            var iterable = ParseExpression();
            var block = ParseBlock();
            
            if (!IsType(TokenType.KeywordElse))
            {
                return new ForStatementNode(target, iterable, block, null, line);
            }

            var elseLine = CurrentToken.LineNumber;
            Next();
            var elseBranch = new BranchStatementNode(null, ParseBlock(), null, elseLine);

            return new ForStatementNode(target, iterable, block, elseBranch, line);
        }

        Node ParseBreakStatement()
        {
            var line = CurrentToken.LineNumber;
            Next(TokenType.KeywordBreak, "Expected 'break' keyword.");
            return new BreakStatementNode(line);
        }

        Node ParseContinueStatement()
        {
            var line = CurrentToken.LineNumber;
            Next(TokenType.KeywordContinue, "Expected 'continue' keyword.");
            return new ContinueStatementNode(line);
        }

        Node ParseReturnStatement()
        {
            var line = CurrentToken.LineNumber;
            Next(TokenType.KeywordReturn, "Expected 'return' keyword.");

            // Void functions always return None, and their calls inside expressions will not cause
            // an error
            return new ReturnStatementNode(IsPrimaryToken() ? ParseExpression() : null, line);
        }

        Node ParseGlobalDeclaration()
        {
            var line = CurrentToken.LineNumber;
            Next(TokenType.KeywordGlobal, "Expected 'global' keyword.");
            return new GlobalNode(ParseDeclarationNameList("global"), line);
        }

        Node ParseNonlocalDeclaration()
        {
            var line = CurrentToken.LineNumber;
            Next(TokenType.KeywordNonlocal, "Expected 'nonlocal' keyword.");
            return new NonLocalNode(ParseDeclarationNameList("nonlocal"), line);
        }

        List<string> ParseDeclarationNameList(string keyword)
        {
            var names = new List<string>();
            var firstToken = Next( 
                TokenType.Identifier, $"Expected identifier after '{keyword}'.");
            
            names.Add(firstToken.Lexeme);

            while (TryNext(TokenType.SymbolComma))
            {
                var nameToken = Next(
                    TokenType.Identifier,
                    $"Expected identifier after ',' in '{keyword}' declaration.");
                
                names.Add(nameToken.Lexeme);
            }

            return names;
        }

        #endregion

        #region Expression Methods

        Node ParseExpression()
        {
            return ParseOr();
        }

        Node ParseOr()
        {
            var leftNode = ParseAnd();

            while (TryNext(TokenType.KeywordOr))
            {
                var opToken = PreviousToken;
                var rightNode = ParseAnd();
                leftNode = new ExpressionNode(
                    ExpressionOperator.Or, leftNode, rightNode, opToken.LineNumber);
            }

            return leftNode;
        }

        Node ParseAnd()
        {
            var leftNode = ParseNot();

            while (TryNext(TokenType.KeywordAnd))
            {
                var opToken = PreviousToken;
                var rightNode = ParseNot();
                leftNode = new ExpressionNode(
                    ExpressionOperator.And, leftNode, rightNode, opToken.LineNumber);
            }

            return leftNode;
        }

        Node ParseNot()
        {
            if (!TryNext(TokenType.KeywordNot))
            {
                return ParseComparison();
            }

            var opToken = PreviousToken;
            return new ExpressionNode(ExpressionOperator.Not, ParseNot(), opToken.LineNumber);
        }

        Node ParseComparison()
        {
            var leftNode = ParseBitOr();
            Node result = null;

            while (true)
            {
                ExpressionOperator @operator;
                int opLine;

                if (TryNext(
                    TokenType.SymbolEqualTo,
                    TokenType.SymbolNotEqual,
                    TokenType.SymbolLess,
                    TokenType.SymbolGreater,
                    TokenType.SymbolLessEqual,
                    TokenType.SymbolGreaterEqual,
                    TokenType.KeywordIn))
                {
                    @operator = MapTokenTypeToBinary(PreviousToken.Type);
                    opLine = PreviousToken.LineNumber;
                }
                else if (IsType(TokenType.KeywordNot) && PeekAhead(TokenType.KeywordIn))
                {
                    opLine = CurrentToken.LineNumber;
                    Next();
                    Next();
                    @operator = ExpressionOperator.NotIn;
                }
                else
                {
                    break;
                }

                var rightNode = ParseBitOr();
                Node comparison = new ExpressionNode(@operator, leftNode, rightNode, opLine);

                if (result == null)
                {
                    result = comparison;
                }
                else
                {
                    result = new ExpressionNode(
                        ExpressionOperator.And, result, comparison, opLine);
                }

                leftNode = rightNode;
            }

            return result ?? leftNode;
        }

        Node ParseBitOr()
        {
            var leftNode = ParseAdd();

            while (TryNext(TokenType.SymbolPipe))
            {
                var opToken = PreviousToken;
                var rightNode = ParseAdd();
                leftNode = new ExpressionNode(
                    ExpressionOperator.BinaryOr, leftNode, rightNode, opToken.LineNumber);
            }

            return leftNode;
        }

        Node ParseAdd()
        {
            var leftNode = ParseTerm();

            while (TryNext(TokenType.SymbolPlus, TokenType.SymbolMinus))
            {
                var opToken = PreviousToken;
                var rightNode = ParseTerm();
                leftNode = new ExpressionNode(
                    MapTokenTypeToBinary(opToken.Type), leftNode, rightNode, opToken.LineNumber);
            }

            return leftNode;
        }

        Node ParseTerm()
        {
            var leftNode = ParseFactor();

            while (TryNext(
                TokenType.SymbolMultiply, 
                TokenType.SymbolDivide, 
                TokenType.SymbolFloorDivide,
                TokenType.SymbolPercent))
            {
                var opToken = PreviousToken;
                var rightNode = ParseFactor();
                leftNode = new ExpressionNode(MapTokenTypeToBinary(opToken.Type), leftNode, rightNode, opToken.LineNumber);
            }

            return leftNode;
        }

        Node ParseFactor()
        {
            if (TryNext(TokenType.SymbolMinus))
            {
                var opToken = PreviousToken;
                return new ExpressionNode(ExpressionOperator.Negate, ParseFactor(), opToken.LineNumber);
            }

            return ParseExponent();
        }

        Node ParseExponent()
        {
            var leftNode = ParsePostfix();

            if (TryNext(TokenType.SymbolExponent))
            {
                var opToken = PreviousToken;
                var rightNode = ParseFactor();
                return new ExpressionNode(ExpressionOperator.Exponentiate, leftNode, rightNode, opToken.LineNumber);
            }

            return leftNode;
        }

        Node ParsePostfix()
        {
            // Accounts for dot notation, indexers, and function calls (e.g. parenthesis, arguments, etc.)
            var node = ParsePrimary();
            var isDone = false;

            do
            {
                switch (CurrentToken.Type)
                {
                    case TokenType.SymbolDot:
                        node = ParseAttributeAccessTail(node);
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

        Node ParseAttributeAccessTail(Node target)
        {
            var dotToken = Next(TokenType.SymbolDot, "Expected '.'.");
            var nameToken = Next(TokenType.Identifier, "Expected attribute name after '.'.");

            return new AttributeAccessNode(target, nameToken.Lexeme, dotToken.LineNumber);
        }

        Node ParseSubscriptTail(Node target)
        {
            var leftBracket = Next(TokenType.SymbolLeftBracket, "Expected '['.");
            var index = ParseSubscriptBody();

            Next(TokenType.SymbolRightBracket, "Expected ']' to close subscript.");

            return new SubscriptNode(target, index, leftBracket.LineNumber);
        }

        Node ParseSubscriptBody()
        {
            Node start = null;
            Node stop = null;
            Node step = null;

            if (!IsType(TokenType.SymbolColon))
            {
                var first = ParseExpression();

                if (!IsType(TokenType.SymbolColon))
                {
                    // Plain index a[i]
                    return first;
                }

                start = first;
            }

            // Positioned on first ':'
            var sliceLine = CurrentToken.LineNumber;
            Next();

            if (!IsType(TokenType.SymbolColon) && !IsType(TokenType.SymbolRightBracket))
            {
                stop = ParseExpression();
            }

            if (TryNext(TokenType.SymbolColon) && !IsType(TokenType.SymbolRightBracket))
            {
                step = ParseExpression();
            }

            return new SubscriptSliceNode(start, stop, step, sliceLine);
        }

        Node ParseInvokeTail(Node callNameNode)
        {
            var leftParen = Next(TokenType.SymbolLeftParen, "Expected '('.");
            var args = new List<Node>();

            if (!IsType(TokenType.SymbolRightParen))
            {
                args.Add(ParseExpression());

                while (TryNext(TokenType.SymbolComma))
                {
                    args.Add(ParseExpression());
                }
            }

            Next(TokenType.SymbolRightParen, "Expected ')' after argument list.");
            return new CallNode(callNameNode, args, leftParen.LineNumber);
        }

        Node ParseListLiteral()
        {
            var leftBracket = Next(TokenType.SymbolLeftBracket, "Expected '['.");
            var elements = new List<Node>();

            if (!IsType(TokenType.SymbolRightBracket))
            {
                elements.Add(ParseExpression());

                // Allow trailing comma: `[1, 2,]`
                while (TryNext(TokenType.SymbolComma) && !IsType(TokenType.SymbolRightBracket))
                {
                    elements.Add(ParseExpression());
                }
            }

            Next(TokenType.SymbolRightBracket, "Expected ']' to close list literal.");
            return new ListNode(elements, leftBracket.LineNumber);
        }

        Node ParseDictLiteral()
        {
            var leftCurly = Next(TokenType.SymbolLeftCurly, "Expected '{'.");
            var keys = new List<Node>();
            var values = new List<Node>();

            if (!IsType(TokenType.SymbolRightCurly))
            {
                ParseDictEntry(keys, values);

                while (TryNext(TokenType.SymbolComma) && !IsType(TokenType.SymbolRightCurly))
                {
                    ParseDictEntry(keys, values);
                }
            }

            Next(TokenType.SymbolRightCurly, "Expected '}' to close dict literal.");
            return new DictionaryNode(keys, values, leftCurly.LineNumber);
        }

        void ParseDictEntry(List<Node> keys, List<Node> values)
        {
            var key = ParseExpression();
            Next(TokenType.SymbolColon, "Expected ':' between dict key and value.");
            var value = ParseExpression();
            keys.Add(key);
            values.Add(value);
        }

        Node ParseFString(FStringTokenPayload payload, int lineNum)
        {
            var exprParts = new List<Node>();

            foreach (var exprSource in payload.ExprSourceParts)
            {
                var subTokens = new Scanner(exprSource).TokenizeSourceCode();
                var subParser = new Parser(subTokens);
                var exprNode = subParser.ParseSingleExpression();
                exprParts.Add(exprNode);
            }

            return new FStringNode(payload.StringParts, exprParts, lineNum);
        }

        Node ParseSingleExpression()
        {
            var node = ParseExpression();

            if (!IsType(TokenType.EndOfCode))
            {
                throw new SyntaxException("f-string: expression must be a single expression.", CurrentToken.LineNumber);
            }

            return node;
        }

        Node ParsePrimary()
        {
            // Note: After adding a new primary token type, remember to update IsPrimaryTokenType() as well. Not doing
            //       so will cause IsPrimaryTokenType() to return false for the new TokenType, which will break certain
            //       statements that behaviors rely on knowing whether an expression is present or not (e.g. return statements).
            switch (CurrentToken.Type)
            {
                case TokenType.Identifier:
                    var idToken = CurrentToken;
                    Next();
                    return new NameNode(idToken.Lexeme, idToken.LineNumber);
                case TokenType.LiteralInt:
                case TokenType.LiteralFloat:
                case TokenType.LiteralStr:
                    var numToken = CurrentToken;
                    Next();
                    return new LiteralNode(numToken.Literal, numToken.LineNumber);
                case TokenType.LiteralFString:
                    var fstrToken = CurrentToken;
                    Next();
                    return ParseFString((FStringTokenPayload)fstrToken.Literal, fstrToken.LineNumber);
                case TokenType.KeywordNone:
                    Next(TokenType.KeywordNone);
                    return new LiteralNode(null, PreviousToken.LineNumber);
                case TokenType.KeywordTrue:
                    Next(TokenType.KeywordTrue);
                    return new LiteralNode(true, PreviousToken.LineNumber);
                case TokenType.KeywordFalse:
                    Next(TokenType.KeywordFalse);
                    return new LiteralNode(false, PreviousToken.LineNumber);
                case TokenType.SymbolLeftParen:
                    Next();
                    var inner = ParseExpression();
                    Next(TokenType.SymbolRightParen);
                    return inner;
                case TokenType.SymbolLeftBracket:
                    return ParseListLiteral();
                case TokenType.SymbolLeftCurly:
                    return ParseDictLiteral();
                default:
                    throw new SyntaxException("Expected expression.", CurrentToken.LineNumber);
            }
        }

        #endregion

        #region Token Helpers

        bool TryNext(TokenType type)
        {
            if (!IsType(type))
            {
                return false;
            }

            Next();
            return true;

        }

        void Next()
        {
            if (CurrentToken.Type != TokenType.EndOfCode)
            {
                _tokenIdx++;
            }
        }

        bool IsType(TokenType type)
        {
            return CurrentToken.Type == type;
        }

        bool PeekAhead(TokenType type, int offset = 1)
        {
            var nextIndex = _tokenIdx + offset;

            // This method will never be called when the current token is EndOfCode, so we don't need to check for out-of-range.
            return _tokens[nextIndex].Type == type;
        }

        bool TryNext(params TokenType[] types)
        {
            foreach (var compareType in types)
            {
                if (IsType(compareType))
                {
                    Next();
                    return true;
                }
            }

            return false;
        }

        Token Next(TokenType type, string message = "")
        {
            if (!IsType(type))
            {
                throw new SyntaxException(message, CurrentToken.LineNumber);
            }

            var token = CurrentToken;
            Next();
            return token;
        }

        bool IsPrimaryToken()
        {
            switch (CurrentToken.Type)
            {
                case TokenType.Identifier:
                case TokenType.LiteralInt:
                case TokenType.LiteralFloat:
                case TokenType.LiteralStr:
                case TokenType.LiteralFString:
                case TokenType.KeywordNone:
                case TokenType.KeywordTrue:
                case TokenType.KeywordFalse:
                case TokenType.KeywordNot:
                case TokenType.SymbolLeftParen:
                case TokenType.SymbolLeftBracket:
                case TokenType.SymbolMinus:
                case TokenType.SymbolLeftCurly:
                    return true;
            }

            return false;
        }

        #endregion

        #region Helper Methods

        Node MakeAssignFromTarget(Node target, Node value, int line)
        {
            switch (target)
            {
                case NameNode nameNode:
                    return new AssignStatementNode(nameNode.Name, value, line);
                case SubscriptNode subscriptNode:
                    return new SubscriptAssignNode(subscriptNode.Target, subscriptNode.Index, value, line);
                case AttributeAccessNode attrNode:
                    return new AttributeAssignNode(attrNode.Target, attrNode.AttributeName, value, line);
            }

            throw new SyntaxException("Invalid assignment target.", line);
        }

        static ExpressionOperator MapTokenTypeToBinary(TokenType type)
        {
            switch (type)
            {
                case TokenType.SymbolPlus:
                    return ExpressionOperator.Add;
                case TokenType.SymbolMinus:
                    return ExpressionOperator.Subtract;
                case TokenType.SymbolMultiply:
                    return ExpressionOperator.Multiply;
                case TokenType.SymbolDivide:
                    return ExpressionOperator.Divide;
                case TokenType.SymbolPercent:
                    return ExpressionOperator.Modulus;
                case TokenType.SymbolExponent:
                    return ExpressionOperator.Exponentiate;
                case TokenType.SymbolFloorDivide:
                    return ExpressionOperator.FloorDivide;
                case TokenType.SymbolEqualTo:
                    return ExpressionOperator.Equal;
                case TokenType.SymbolNotEqual:
                    return ExpressionOperator.NotEqual;
                case TokenType.SymbolLess:
                    return ExpressionOperator.Less;
                case TokenType.SymbolGreater:
                    return ExpressionOperator.Greater;
                case TokenType.SymbolLessEqual:
                    return ExpressionOperator.LessEqual;
                case TokenType.SymbolGreaterEqual:
                    return ExpressionOperator.GreaterEqual;
                case TokenType.KeywordAnd:
                    return ExpressionOperator.And;
                case TokenType.KeywordOr:
                    return ExpressionOperator.Or;
                case TokenType.SymbolPipe:
                    return ExpressionOperator.BinaryOr;
                case TokenType.KeywordIn:
                    return ExpressionOperator.In;
                default:
                    throw new InvalidOperationException();
            }
        }

        #endregion

    }
}
