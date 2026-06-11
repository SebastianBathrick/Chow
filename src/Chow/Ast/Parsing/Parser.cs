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
        
        readonly ITokenStream _tokens;

        #region Main Methods
    
        /// <summary>Initializes a new instance with the tokens it will analyze.</summary>
        /// <param name="tokens">The stream of tokens used to build an AST upon calling
        /// <see cref="BuildAst"/>.</param>
        public Parser(ITokenStream tokens)
        {
            _tokens = tokens;
        }
        
        public Node BuildAst()
        {
            // Elements represent top-level statements
            var statementList = new List<Node>();
            
            // Iterate until reaching the 'end of code' marker
            while(!_tokens.IsMatch(TokenType.EndOfCode))
            {
                if (!_tokens.TryConsumeMatch(TokenType.Newline))
                {
                    statementList.Add(ParseStatement());
                }
            }

            // It's assumed the final token always an 'end of code' marker
            _tokens.ConsumeMatch(TokenType.EndOfCode);
            var block = new BlockNode(statementList, ModuleNodeLineNumber);
            return new ModuleNode(block);
        }

        #endregion

        #region Statement Methods

        Node ParseBlock()
        {
            _tokens.ConsumeMatch(TokenType.SymbolColon);
            _tokens.ConsumeMatch(TokenType.Newline);

            var indentToken = _tokens.ConsumeMatch(TokenType.Indent);
            
            var statements = new List<Node> { ParseStatement() };

            
            
            _tokens.TryConsumeMatch(TokenType.Newline);

            while (!_tokens.IsMatch(TokenType.Dedent))
            {
                if (_tokens.IsMatch(TokenType.Newline))
                {
                    _tokens.Consume();
                    continue;
                }

                statements.Add(ParseStatement());
                _tokens.TryConsumeMatch(TokenType.Newline);
            }

            _tokens.ConsumeMatch(TokenType.Dedent);
            return new BlockNode(statements, indentToken.LineNumber);
        }

        Node ParseStatement()
        {
            switch (_tokens.Peek())
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
                    break;
                default:
                    _tokens.ConsumeMatch(TokenType.Identifier);
                    break;
            }

            // Parse expression first; if an '=' follows, convert the LHS into the appropriate
            // assignment node. Otherwise, this is a standalone expression statement (result
            // discarded or routed to hook).
            var lhs = ParseExpression();

            if (!_tokens.IsMatch(TokenType.SymbolAssign))
            {
                return new ExpressionStatementNode(lhs, lhs.LineNumber);
            }

            var eqLine = _tokens.Consume().LineNumber;
            var rhs = ParseExpression();
            return MakeAssignFromTarget(lhs, rhs, eqLine);
        }

        Node ParseFunctionDefinition()
        {
            var defToken = _tokens.ConsumeMatch(TokenType.KeywordDef);

            var nameToken = _tokens.ConsumeMatch(TokenType.Identifier);

            _tokens.ConsumeMatch(TokenType.SymbolLeftParen);

            var paramList = new List<Node>();

            if (!_tokens.IsMatch(TokenType.SymbolRightParen))
            {
                var paramToken = _tokens.ConsumeMatch(TokenType.Identifier);
                
                paramList.Add(new NameNode(paramToken.Lexeme, paramToken.LineNumber));

                while (_tokens.TryConsumeMatch(TokenType.SymbolComma))
                {
                    paramToken = _tokens.ConsumeMatch(TokenType.Identifier);
                    
                    paramList.Add(new NameNode(paramToken.Lexeme, paramToken.LineNumber));
                }
            }

            _tokens.ConsumeMatch(TokenType.SymbolRightParen);
            return new FunctionNode(nameToken.Lexeme, paramList, ParseBlock(), defToken.LineNumber);
        }

        Node ParseIfStatement()
        {
            var ifToken = _tokens.ConsumeMatch(TokenType.KeywordIf);
            return new IfStatementNode(
                ParseExpression(), ParseBlock(), ParseBranchStatement(), ifToken.LineNumber);
        }

        Node ParseBranchStatement()
        {
            if (_tokens.IsMatch(TokenType.KeywordElif))
            {
                var elifToken = _tokens.Consume();
                return new BranchStatementNode(
                    ParseExpression(), ParseBlock(), ParseBranchStatement(), elifToken.LineNumber);
            }

            if (!_tokens.IsMatch(TokenType.KeywordElse))
            {
                return null;
            }

            {
                var elseToken = _tokens.Consume();
                return new BranchStatementNode(null, ParseBlock(), null, elseToken.LineNumber);
            }
        }

        Node ParseWhileStatement()
        {
            var whileToken = _tokens.ConsumeMatch(TokenType.KeywordWhile);
            return new WhileStatementNode(ParseExpression(), ParseBlock(), whileToken.LineNumber);
        }

        Node ParseForStatement()
        {
            var forToken = _tokens.ConsumeMatch(TokenType.KeywordFor);

            var targetToken = _tokens.ConsumeMatch(TokenType.Identifier);
            var target = new NameNode(targetToken.Lexeme, targetToken.LineNumber);

            _tokens.ConsumeMatch(TokenType.KeywordIn);

            var iterable = ParseExpression();
            var block = ParseBlock();
            
            if (!_tokens.IsMatch(TokenType.KeywordElse))
            {
                return new ForStatementNode(target, iterable, block, null, forToken.LineNumber);
            }

            var elseToken = _tokens.Consume();
            var elseBranch = new BranchStatementNode(null, ParseBlock(), null, elseToken.LineNumber);

            return new ForStatementNode(target, iterable, block, elseBranch, forToken.LineNumber);
        }

        Node ParseBreakStatement()
        {
            var breakToken = _tokens.ConsumeMatch(TokenType.KeywordBreak);
            return new BreakStatementNode(breakToken.LineNumber);
        }

        Node ParseContinueStatement()
        {
            var continueToken = _tokens.ConsumeMatch(TokenType.KeywordContinue);
            return new ContinueStatementNode(continueToken.LineNumber);
        }

        Node ParseReturnStatement()
        {
            var returnToken = _tokens.ConsumeMatch(TokenType.KeywordReturn);

            // Void functions always return None, and their calls inside expressions will not cause
            // an error
            return new ReturnStatementNode(
                _tokens.IsMatch(
                    TokenType.Identifier,
                    TokenType.LiteralInt,
                    TokenType.LiteralFloat,
                    TokenType.LiteralStr,
                    TokenType.LiteralFString,
                    TokenType.KeywordNone,
                    TokenType.KeywordTrue,
                    TokenType.KeywordFalse,
                    TokenType.KeywordNot,
                    TokenType.SymbolLeftParen,
                    TokenType.SymbolLeftBracket,
                    TokenType.SymbolMinus,
                    TokenType.SymbolLeftCurly)
                    ? ParseExpression()
                    : null,
                returnToken.LineNumber);
        }

        Node ParseGlobalDeclaration()
        {
            var globalToken = _tokens.ConsumeMatch(TokenType.KeywordGlobal);
            return new GlobalNode(ParseDeclarationNameList("global"), globalToken.LineNumber);
        }

        Node ParseNonlocalDeclaration()
        {
            var nonlocalToken = _tokens.ConsumeMatch(TokenType.KeywordNonlocal);
            return new NonLocalNode(ParseDeclarationNameList("nonlocal"), nonlocalToken.LineNumber);
        }

        List<string> ParseDeclarationNameList(string keyword)
        {
            var names = new List<string>();
            var firstToken = _tokens.ConsumeMatch(TokenType.Identifier);
            
            names.Add(firstToken.Lexeme);

            while (_tokens.TryConsumeMatch(TokenType.SymbolComma))
            {
                var nameToken = _tokens.ConsumeMatch(TokenType.Identifier);
                
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

            while (_tokens.IsMatch(TokenType.KeywordOr))
            {
                var opToken = _tokens.Consume();
                var rightNode = ParseAnd();
                leftNode = new ExpressionNode(
                    ExpressionOperator.Or, leftNode, rightNode, opToken.LineNumber);
            }

            return leftNode;
        }

        Node ParseAnd()
        {
            var leftNode = ParseNot();

            while (_tokens.IsMatch(TokenType.KeywordAnd))
            {
                var opToken = _tokens.Consume();
                var rightNode = ParseNot();
                leftNode = new ExpressionNode(
                    ExpressionOperator.And, leftNode, rightNode, opToken.LineNumber);
            }

            return leftNode;
        }

        Node ParseNot()
        {
            if (!_tokens.IsMatch(TokenType.KeywordNot))
            {
                return ParseComparison();
            }

            var opToken = _tokens.Consume();
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

                if (_tokens.IsMatch(
                    TokenType.SymbolEqualTo,
                    TokenType.SymbolNotEqual,
                    TokenType.SymbolLess,
                    TokenType.SymbolGreater,
                    TokenType.SymbolLessEqual,
                    TokenType.SymbolGreaterEqual,
                    TokenType.KeywordIn))
                {
                    var opToken = _tokens.Consume();
                    @operator = MapTokenTypeToBinary(opToken.Type);
                    opLine = opToken.LineNumber;
                }
                else if (_tokens.IsMatch(TokenType.KeywordNot) && _tokens.IsNextMatch(TokenType.KeywordIn))
                {
                    opLine = _tokens.Consume().LineNumber;
                    _tokens.ConsumeMatch(TokenType.KeywordIn);
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

            while (_tokens.IsMatch(TokenType.SymbolPipe))
            {
                var opToken = _tokens.Consume();
                var rightNode = ParseAdd();
                leftNode = new ExpressionNode(
                    ExpressionOperator.BinaryOr, leftNode, rightNode, opToken.LineNumber);
            }

            return leftNode;
        }

        Node ParseAdd()
        {
            var leftNode = ParseTerm();

            while (_tokens.IsMatch(TokenType.SymbolPlus, TokenType.SymbolMinus))
            {
                var opToken = _tokens.Consume();
                var rightNode = ParseTerm();
                leftNode = new ExpressionNode(
                    MapTokenTypeToBinary(opToken.Type), leftNode, rightNode, opToken.LineNumber);
            }

            return leftNode;
        }

        Node ParseTerm()
        {
            var leftNode = ParseFactor();

            while (_tokens.IsMatch(
                TokenType.SymbolMultiply, 
                TokenType.SymbolDivide, 
                TokenType.SymbolFloorDivide,
                TokenType.SymbolPercent))
            {
                var opToken = _tokens.Consume();
                var rightNode = ParseFactor();
                leftNode = new ExpressionNode(MapTokenTypeToBinary(opToken.Type), leftNode, rightNode, opToken.LineNumber);
            }

            return leftNode;
        }

        Node ParseFactor()
        {
            if (_tokens.IsMatch(TokenType.SymbolMinus))
            {
                var opToken = _tokens.Consume();
                return new ExpressionNode(ExpressionOperator.Negate, ParseFactor(), opToken.LineNumber);
            }

            return ParseExponent();
        }

        Node ParseExponent()
        {
            var leftNode = ParsePostfix();

            if (_tokens.IsMatch(TokenType.SymbolExponent))
            {
                var opToken = _tokens.Consume();
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
                if (_tokens.IsMatch(TokenType.SymbolDot))
                {
                    node = ParseAttributeAccessTail(node);
                }
                else if (_tokens.IsMatch(TokenType.SymbolLeftBracket))
                {
                    node = ParseSubscriptTail(node);
                }
                else if (_tokens.IsMatch(TokenType.SymbolLeftParen))
                {
                    node = ParseInvokeTail(node);
                }
                else
                {
                    isDone = true;
                }
            }
            while (!isDone);

            return node;
        }

        Node ParseAttributeAccessTail(Node target)
        {
            var dotToken = _tokens.ConsumeMatch(TokenType.SymbolDot);
            var nameToken = _tokens.ConsumeMatch(TokenType.Identifier);

            return new AttributeAccessNode(target, nameToken.Lexeme, dotToken.LineNumber);
        }

        Node ParseSubscriptTail(Node target)
        {
            var leftBracket = _tokens.ConsumeMatch(TokenType.SymbolLeftBracket);
            var index = ParseSubscriptBody();

            _tokens.ConsumeMatch(TokenType.SymbolRightBracket);

            return new SubscriptNode(target, index, leftBracket.LineNumber);
        }

        Node ParseSubscriptBody()
        {
            Node start = null;
            Node stop = null;
            Node step = null;

            if (!_tokens.IsMatch(TokenType.SymbolColon))
            {
                var first = ParseExpression();

                if (!_tokens.IsMatch(TokenType.SymbolColon))
                {
                    // Plain index a[i]
                    return first;
                }

                start = first;
            }

            // Positioned on first ':'
            var sliceLine = _tokens.ConsumeMatch(TokenType.SymbolColon).LineNumber;

            if (!_tokens.IsMatch(TokenType.SymbolColon) && !_tokens.IsMatch(TokenType.SymbolRightBracket))
            {
                stop = ParseExpression();
            }

            if (_tokens.TryConsumeMatch(TokenType.SymbolColon) && !_tokens.IsMatch(TokenType.SymbolRightBracket))
            {
                step = ParseExpression();
            }

            return new SubscriptSliceNode(start, stop, step, sliceLine);
        }

        Node ParseInvokeTail(Node callNameNode)
        {
            var leftParen = _tokens.ConsumeMatch(TokenType.SymbolLeftParen);
            var args = new List<Node>();

            if (!_tokens.IsMatch(TokenType.SymbolRightParen))
            {
                args.Add(ParseExpression());

                while (_tokens.TryConsumeMatch(TokenType.SymbolComma))
                {
                    args.Add(ParseExpression());
                }
            }

            _tokens.ConsumeMatch(TokenType.SymbolRightParen);
            return new CallNode(callNameNode, args, leftParen.LineNumber);
        }

        Node ParseListLiteral()
        {
            var leftBracket = _tokens.ConsumeMatch(TokenType.SymbolLeftBracket);
            var elements = new List<Node>();

            if (!_tokens.IsMatch(TokenType.SymbolRightBracket))
            {
                elements.Add(ParseExpression());

                // Allow trailing comma: `[1, 2,]`
                while (_tokens.TryConsumeMatch(TokenType.SymbolComma) && !_tokens.IsMatch(TokenType.SymbolRightBracket))
                {
                    elements.Add(ParseExpression());
                }
            }

            _tokens.ConsumeMatch(TokenType.SymbolRightBracket);
            return new ListNode(elements, leftBracket.LineNumber);
        }

        Node ParseDictLiteral()
        {
            var leftCurly = _tokens.ConsumeMatch(TokenType.SymbolLeftCurly);
            var keys = new List<Node>();
            var values = new List<Node>();

            if (!_tokens.IsMatch(TokenType.SymbolRightCurly))
            {
                ParseDictEntry(keys, values);

                while (_tokens.TryConsumeMatch(TokenType.SymbolComma) && !_tokens.IsMatch(TokenType.SymbolRightCurly))
                {
                    ParseDictEntry(keys, values);
                }
            }

            _tokens.ConsumeMatch(TokenType.SymbolRightCurly);
            return new DictionaryNode(keys, values, leftCurly.LineNumber);
        }

        void ParseDictEntry(List<Node> keys, List<Node> values)
        {
            var key = ParseExpression();
            _tokens.ConsumeMatch(TokenType.SymbolColon);
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
                var subParser = new Parser(new TokenStream(subTokens));
                var exprNode = subParser.ParseSingleExpression();
                exprParts.Add(exprNode);
            }

            return new FStringNode(payload.StringParts, exprParts, lineNum);
        }

        Node ParseSingleExpression()
        {
            var node = ParseExpression();

            if (!_tokens.IsMatch(TokenType.EndOfCode))
            {
                _tokens.ConsumeMatch(TokenType.EndOfCode);
            }

            return node;
        }

        Node ParsePrimary()
        {
            // Note: After adding a new primary token type, remember to update expression-start
            // checks that gate statements and return expressions.
            switch (_tokens.Peek())
            {
                case TokenType.Identifier:
                    var idToken = _tokens.Consume();
                    return new NameNode(idToken.Lexeme, idToken.LineNumber);
                case TokenType.LiteralInt:
                case TokenType.LiteralFloat:
                case TokenType.LiteralStr:
                    var numToken = _tokens.Consume();
                    return new LiteralNode(numToken.Literal, numToken.LineNumber);
                case TokenType.LiteralFString:
                    var fstrToken = _tokens.Consume();
                    return ParseFString((FStringTokenPayload)fstrToken.Literal, fstrToken.LineNumber);
                case TokenType.KeywordNone:
                    var noneToken = _tokens.ConsumeMatch(TokenType.KeywordNone);
                    return new LiteralNode(null, noneToken.LineNumber);
                case TokenType.KeywordTrue:
                    var trueToken = _tokens.ConsumeMatch(TokenType.KeywordTrue);
                    return new LiteralNode(true, trueToken.LineNumber);
                case TokenType.KeywordFalse:
                    var falseToken = _tokens.ConsumeMatch(TokenType.KeywordFalse);
                    return new LiteralNode(false, falseToken.LineNumber);
                case TokenType.SymbolLeftParen:
                    _tokens.Consume();
                    var inner = ParseExpression();
                    _tokens.ConsumeMatch(TokenType.SymbolRightParen);
                    return inner;
                case TokenType.SymbolLeftBracket:
                    return ParseListLiteral();
                case TokenType.SymbolLeftCurly:
                    return ParseDictLiteral();
                default:
                    _tokens.ConsumeMatch(TokenType.Identifier);
                    return null;
            }
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
