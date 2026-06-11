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
                case TokenType.Name:
                    return ParseAssignStatement(lineNum);
            }
            
            return ParseExpressionStatement(lineNum);
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

            if (targetNode is NameNode nameNode)
            {
                return new AssignStatementNode(nameNode.Name, assignExpr, lineNum);
            }

            if (targetNode is AttributeAccessNode attrAccessNode)
            {
                // TODO: Switch out any strings stored by Nodes with NameNodes
                    return new AttributeAssignNode(
                        attrAccessNode.Target , attrAccessNode.AttributeName, assignExpr, lineNum);
            }
            
            if (targetNode is SubscriptNode subscriptNode)
            {
                return new SubscriptAssignNode(
                    subscriptNode.Target, subscriptNode.Index, assignExpr, lineNum);
            }

            throw new SyntaxException("assignment", lineNum);
        }

        Node ParseExpressionStatement(int lineNumber)
        {
            var expr = ParseExpression();
            return new ExpressionStatementNode(expr, lineNumber);
        }

        Node ParseFunctionDefinition()
        {
            var defToken = _tokens.ConsumeMatch(TokenType.KeywordDef);
            var nameToken = _tokens.ConsumeMatch(TokenType.Name);

            _tokens.ConsumeMatch(TokenType.SymbolLeftParen);

            var paramList = new List<Node>();

            if (!_tokens.IsMatch(TokenType.SymbolRightParen))
            {
                var paramToken = _tokens.ConsumeMatch(TokenType.Name);
                
                paramList.Add(new NameNode(paramToken.Lexeme, paramToken.LineNumber));

                while (_tokens.TryConsumeMatch(TokenType.SymbolComma))
                {
                    paramToken = _tokens.ConsumeMatch(TokenType.Name);
                    
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

            var targetToken = _tokens.ConsumeMatch(TokenType.Name);
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
                    TokenType.Name,
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
            var firstToken = _tokens.ConsumeMatch(TokenType.Name);
            
            names.Add(firstToken.Lexeme);

            while (_tokens.TryConsumeMatch(TokenType.SymbolComma))
            {
                var nameToken = _tokens.ConsumeMatch(TokenType.Name);
                
                names.Add(nameToken.Lexeme);
            }

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
                leftNode = new ExpressionNode(
                    MapTokenTypeToBinary(opToken.Type), leftNode, rightNode, opToken.LineNumber);
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

        Node ParseComparison()
        {
            var leftNode = ParseBitOr();
            Node result = null;

            while (true)
            {
                Operator @operator;
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
                    @operator = Operator.NotIn;
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
                var opToken = _tokens.Consume();
                var rightNode = ParseFactor();
                return new ExpressionNode(Operator.Exponentiate, leftNode, rightNode, opToken.LineNumber);
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
            var nameToken = _tokens.ConsumeMatch(TokenType.Name);

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

        static Operator MapTokenTypeToBinary(TokenType type)
        {
            switch (type)
            {
                case TokenType.SymbolPlus:
                    return Operator.Add;
                case TokenType.SymbolMinus:
                    return Operator.Subtract;
                case TokenType.SymbolMultiply:
                    return Operator.Multiply;
                case TokenType.SymbolDivide:
                    return Operator.Divide;
                case TokenType.SymbolPercent:
                    return Operator.Modulus;
                case TokenType.SymbolExponent:
                    return Operator.Exponentiate;
                case TokenType.SymbolFloorDivide:
                    return Operator.FloorDivide;
                case TokenType.SymbolEqualTo:
                    return Operator.Equal;
                case TokenType.SymbolNotEqual:
                    return Operator.NotEqual;
                case TokenType.SymbolLess:
                    return Operator.Less;
                case TokenType.SymbolGreater:
                    return Operator.Greater;
                case TokenType.SymbolLessEqual:
                    return Operator.LessEqual;
                case TokenType.SymbolGreaterEqual:
                    return Operator.GreaterEqual;
                case TokenType.KeywordAnd:
                    return Operator.And;
                case TokenType.KeywordOr:
                    return Operator.Or;
                case TokenType.SymbolPipe:
                    return Operator.BinaryOr;
                case TokenType.KeywordIn:
                    return Operator.In;
                default:
                    throw new InvalidOperationException();
            }
        }

        #endregion

    }
}
