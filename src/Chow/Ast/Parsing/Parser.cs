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
            _tokens.ConsumeMatches(TokenType.SymbolColon, TokenType.Newline);
            var indentToken = _tokens.ConsumeMatch(TokenType.Indent);
            List<Node> statements = new List<Node>();
            
            while (!_tokens.IsMatch(TokenType.Dedent))
            {
                if (_tokens.TryConsumeMatch(TokenType.Newline))
                {
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
            var lineNum = _tokens.LineNumber;

            switch (_tokens.Peek())
            {
                case TokenType.KeywordReturn:
                    return ParseReturnStatement(lineNum);
                case TokenType.KeywordIf:
                    return ParseIfStatement(lineNum);
                case TokenType.KeywordDef:
                    return ParseFunctionDefinition(lineNum);
                case TokenType.KeywordWhile:
                    return ParseWhileStatement(lineNum);
                case TokenType.KeywordFor:
                    return ParseForStatement(lineNum);
                case TokenType.KeywordBreak:
                    return ParseBreakStatement(lineNum);
                case TokenType.KeywordContinue:
                    return ParseContinueStatement(lineNum);
                case TokenType.KeywordGlobal:
                    return ParseGlobalDeclaration(lineNum);
                case TokenType.KeywordNonlocal:
                    return ParseNonlocalDeclaration(lineNum);
                case TokenType.Name:
                    return ParseAssignStatement(lineNum);
                default:
                    return ParseExpressionStatement(lineNum);
            }
        }

        Node ParseAssignStatement(int lineNum)
        {
            var targetNode = ParseExpression();

            // Targets like subscripts and attribute accesses span an arbitrary number of
            // tokens, so '=' can only be checked for after parsing the target expression.
            if (!_tokens.TryConsumeMatch(TokenType.SymbolAssign))
            {
                return new ExpressionStatementNode(targetNode, lineNum);
            }

            var assignExpr = ParseExpression();

            switch (targetNode)
            {
                case NameNode nameNode:
                    return new AssignStatementNode(nameNode.Name, assignExpr, lineNum);
                
                case AttributeAccessNode attrAccess:
                    return new AttributeAssignNode(
                        attrAccess.Target , attrAccess.AttributeName, assignExpr, lineNum);
                
                case SubscriptNode subscript:
                    return new SubscriptAssignNode(
                        subscript.Target, subscript.Index, assignExpr, lineNum);
                
                default:
                    throw new SyntaxException("assignment", lineNum);
            }

        }

        Node ParseExpressionStatement(int lineNum)
        {
            return new ExpressionStatementNode(ParseExpression(), lineNum);
        }

        Node ParseFunctionDefinition(int lineNum)
        {
            _tokens.Consume();
            
            var functionName = _tokens.ConsumeMatch(TokenType.Name).Lexeme;
            _tokens.ConsumeMatch(TokenType.SymbolLeftParen);
            
            var paramList = new List<Node>();

            if (!_tokens.IsMatch(TokenType.SymbolRightParen))
            {
                // TODO: Refactor to not use an action
                ParseCommaSeparatedElements(() =>
                {
                    var paramToken = _tokens.ConsumeMatch(TokenType.Name);
                    paramList.Add(new NameNode(paramToken.Lexeme, paramToken.LineNumber));
                });
            }

            _tokens.ConsumeMatch(TokenType.SymbolRightParen);
            return new FunctionNode(functionName, paramList, ParseBlock(), lineNum);
        }

        Node ParseIfStatement(int lineNum)
        {
            _tokens.Consume();
            var expr = ParseExpression();
            var block = ParseBlock();
            var branch = ParseBranchStatement();
            return new IfStatementNode(expr, block, branch, lineNum);
        }

        Node ParseBranchStatement()
        {
            var lineNum = _tokens.LineNumber;

            if (!_tokens.TryConsumeMatch(TokenType.KeywordElif))
            {
                return _tokens.TryConsumeMatch(TokenType.KeywordElse)
                    ? new BranchStatementNode(null, ParseBlock(), null, lineNum) : null;
            }

            var expr = ParseExpression();
            var block = ParseBlock();
            var branch = ParseBranchStatement();
            return new BranchStatementNode(expr, block, branch, lineNum);
        }

        Node ParseWhileStatement(int lineNum)
        {
            _tokens.Consume();
            return new WhileStatementNode(ParseExpression(), ParseBlock(), lineNum);
        }

        Node ParseForStatement(int lineNum)
        {
            _tokens.Consume();
            
            var targetName = _tokens.ConsumeMatch(TokenType.Name).Lexeme;
            var target = new NameNode(targetName, lineNum);
            
            _tokens.ConsumeMatch(TokenType.KeywordIn);

            var iterable = ParseExpression();
            var block = ParseBlock();
            
            Node elseBranch = null;
            
            if (_tokens.IsMatch(TokenType.KeywordElse))
            {
                var elseLineNum = _tokens.Consume().LineNumber;
                elseBranch = new BranchStatementNode(null, ParseBlock(), null, elseLineNum);
            }
            
            return new ForStatementNode(target, iterable, block, elseBranch, lineNum);
        }

        Node ParseBreakStatement(int lineNum)
        {
            _tokens.Consume();
            return new BreakStatementNode(lineNum);
        }

        Node ParseContinueStatement(int lineNum)
        {
            _tokens.Consume();
            return new ContinueStatementNode(lineNum);
        }

        Node ParseReturnStatement(int lineNum)
        {
            _tokens.Consume();

            // Void functions always return None, and their calls inside expressions will not cause
            // an error
            var expr = SyntaxMaps.IsExpressionStart(_tokens.Peek()) ? ParseExpression() : null;
            return new ReturnStatementNode(expr, lineNum);
        }

        Node ParseGlobalDeclaration(int lineNum)
        {
            _tokens.Consume();
            return new GlobalNode(ParseDeclarationNameList(), lineNum);
        }

        Node ParseNonlocalDeclaration(int lineNum)
        {
            _tokens.Consume();
            return new NonLocalNode(ParseDeclarationNameList(), lineNum);
        }

        List<string> ParseDeclarationNameList()
        {
            var names = new List<string>();

            // TODO: Refactor to remove Action
            ParseCommaSeparatedElements(() => 
                names.Add(_tokens.ConsumeMatch(TokenType.Name).Lexeme));

            return names;
        }

        #endregion

        #region Expression Methods

        Node ParseExpression()
        {
            return ParseBinaryLevel(ParseAnd, TokenType.KeywordOr);
        }

        /// <summary>Parses a left-associative binary operator precedence level.</summary>
        /// <param name="parseOperand">Parses an operand at the next-higher precedence
        /// level.</param>
        /// <param name="operatorTypes">The operator token types belonging to this level.</param>
        Node ParseBinaryLevel(Func<Node> parseOperand, params TokenType[] operatorTypes)
        {
            var leftNode = parseOperand();

            while (_tokens.IsMatch(operatorTypes))
            {
                var opToken = _tokens.Consume();
                var rightNode = parseOperand();
                var binaryOp = SyntaxMaps.ToBinaryOperator(opToken.Type);
                
                leftNode = new ExpressionNode(binaryOp, leftNode, rightNode, opToken.LineNumber);
            }

            return leftNode;
        }
        
        Node ParseAnd()
        {
            return ParseBinaryLevel(ParseNot, TokenType.KeywordAnd);
        }

        Node ParseNot()
        {
            if (!_tokens.IsMatch(TokenType.KeywordNot))
            {
                return ParseComparison();
            }

            var opToken = _tokens.Consume();
            return new ExpressionNode(Operator.Not, ParseNot(), opToken.LineNumber);
        }


        bool IsNotInOperatorNext()
        {
            return _tokens.IsMatch(TokenType.KeywordNot) && _tokens.IsNextMatch(TokenType.KeywordIn);
        }

        Node ParseComparison()
        {
            var leftNode = ParseBitOr();
            Node result = null;

            while (SyntaxMaps.IsComparisonOperator(_tokens.Peek()) || IsNotInOperatorNext())
            {
                Operator op;
                int opLine;

                if (SyntaxMaps.IsComparisonOperator(_tokens.Peek()))
                {
                    var opToken = _tokens.Consume();
                    op = SyntaxMaps.ToBinaryOperator(opToken.Type);
                    opLine = opToken.LineNumber;
                }
                else
                {
                    opLine = _tokens.Consume().LineNumber;
                    _tokens.ConsumeMatch(TokenType.KeywordIn);
                    op = Operator.NotIn;
                }

                var rightNode = ParseBitOr();
                Node comparison = new ExpressionNode(op, leftNode, rightNode, opLine);

                if (result == null)
                {
                    result = comparison;
                }
                else
                {
                    result = new ExpressionNode(Operator.And, result, comparison, opLine);
                }

                leftNode = rightNode;
            }

            return result ?? leftNode;
        }

        Node ParseBitOr()
        {
            return ParseBinaryLevel(ParseAdd, TokenType.SymbolPipe);
        }

        Node ParseAdd()
        {
            return ParseBinaryLevel(ParseTerm, TokenType.SymbolPlus, TokenType.SymbolMinus);
        }

        Node ParseTerm()
        {
            return ParseBinaryLevel(
                ParseFactor,
                TokenType.SymbolMultiply,
                TokenType.SymbolDivide,
                TokenType.SymbolFloorDivide,
                TokenType.SymbolPercent);
        }

        Node ParseFactor()
        {
            if (_tokens.IsMatch(TokenType.SymbolMinus))
            {
                var opToken = _tokens.Consume();
                return new ExpressionNode(Operator.Negate, ParseFactor(), opToken.LineNumber);
            }

            return ParseExponent();
        }

        Node ParseExponent()
        {
            var leftNode = ParsePostfix();

            if (_tokens.IsMatch(TokenType.SymbolExponent))
            {
                var lineNum = _tokens.Consume().LineNumber;
                var rightNode = ParseFactor();
                return new ExpressionNode(Operator.Exponentiate, leftNode, rightNode, lineNum);
            }

            return leftNode;
        }

        Node ParsePostfix()
        {
            // Accounts for dot notation, indexers, and function calls (e.g., parenthesis, arguments, etc.)
            var node = ParsePrimary();
            var isDone = false;

            do
            {
                switch (_tokens.Peek())
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
            var lineNum = _tokens.ConsumeMatch(TokenType.SymbolDot).LineNumber;
            var nameToken = _tokens.ConsumeMatch(TokenType.Name);

            return new AttributeAccessNode(target, nameToken.Lexeme, lineNum);
        }

        Node ParseSubscriptTail(Node target)
        {
            var linNum = _tokens.ConsumeMatch(TokenType.SymbolLeftBracket).LineNumber;
            var index = ParseSubscriptBody();

            _tokens.ConsumeMatch(TokenType.SymbolRightBracket);

            return new SubscriptNode(target, index, linNum);
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
            var lineNum = _tokens.ConsumeMatch(TokenType.SymbolLeftParen).LineNumber;
            var args = new List<Node>();

            if (!_tokens.IsMatch(TokenType.SymbolRightParen))
            {
                ParseCommaSeparatedElements(() => 
                    args.Add(ParseExpression()));
            }

            _tokens.ConsumeMatch(TokenType.SymbolRightParen);
            return new CallNode(callNameNode, args, lineNum);
        }

        Node ParseListLiteral()
        {
            var lineNum = _tokens.ConsumeMatch(TokenType.SymbolLeftBracket).LineNumber;
            var elements = new List<Node>();

            if (!_tokens.IsMatch(TokenType.SymbolRightBracket))
            {
                // Allow trailing comma: `[1, 2,]`
                ParseCommaSeparatedElements(() => 
                    elements.Add(ParseExpression()), TokenType.SymbolRightBracket);
            }

            _tokens.ConsumeMatch(TokenType.SymbolRightBracket);
            return new ListNode(elements, lineNum);
        }

        Node ParseDictLiteral()
        {
            var lineNum = _tokens.ConsumeMatch(TokenType.SymbolLeftCurly).LineNumber;
            var keys = new List<Node>();
            var values = new List<Node>();

            if (!_tokens.IsMatch(TokenType.SymbolRightCurly))
            {
                ParseCommaSeparatedElements(() => 
                    ParseDictEntry(keys, values), TokenType.SymbolRightCurly);
            }

            _tokens.ConsumeMatch(TokenType.SymbolRightCurly);
            return new DictionaryNode(keys, values, lineNum);
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
            // Note: After adding a new primary token type, remember to add it to
            // SyntaxMaps.ExpressionStartTypes.
            switch (_tokens.Peek())
            {
                case TokenType.Name:
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
                    _tokens.ConsumeMatch(TokenType.Name);
                    return null;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>Parses one or more comma-separated elements.</summary>
        /// <param name="parseElement">Parses a single element and stores it.</param>
        /// <param name="closingType">The token type that may follow a trailing comma to end the
        /// list, or <c>null</c> if a trailing comma is not allowed.</param>
        void ParseCommaSeparatedElements(Action parseElement, TokenType? closingType = null)
        {
            parseElement();

            while (_tokens.TryConsumeMatch(TokenType.SymbolComma)
                && !(closingType.HasValue && _tokens.IsMatch(closingType.Value)))
            {
                parseElement();
            }
        }

        #endregion

    }
}
