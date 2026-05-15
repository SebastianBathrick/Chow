using Chow.Interpreter.Exceptions;
using Chow.Interpreter.Tokens;
using System;
using System.Collections.Generic;
using Chow.Interpreter.SyntaxTrees;
using Chow.Interpreter.SyntaxTrees.Attributes;
using Chow.Interpreter.SyntaxTrees.Expressions;
using Chow.Interpreter.SyntaxTrees.Literals;
using Chow.Interpreter.SyntaxTrees.Scope;
using Chow.Interpreter.SyntaxTrees.Statements;
using Chow.Interpreter.SyntaxTrees.Subscripts;

namespace Chow.Interpreter
{
    /// <summary>
    /// Instances perform syntax analysis on a list of <see cref="Token"/>. The client provides the list of tokens from
    /// the interpreter's scanning phase via an argument passed to the Parser instance's constructor.
    /// <para>
    /// To begin syntax analysis, the client calls <see cref="BuildTree"/>, which iterates over each token to determine
    /// whether the source code's grammar is valid and which constructs it is trying to define. While doing so, it builds
    /// an abstract syntax tree that outlines the constructs and any relevant information from the tokens. Once the tree
    /// is complete, BuildTree returns a <see cref="Node"/> object representing the root of the abstract syntax tree.
    /// After this point, the client should discard the Parser instance, because it will be considered dirty.
    /// </para>
    /// </summary>
    sealed class Parser
    {
        readonly List<Token> _tokens;
        int _tokenIdx;

        Token CurrentToken => _tokens[_tokenIdx];
        Token PreviousToken => _tokens[_tokenIdx - 1];

        #region Primary Methods

        /// <summary>
        /// Initializes a new instance of the <see cref="Parser"/> class with the tokens to analyze.
        /// </summary>
        /// <param name="tokens">The <see cref="Token"/> list produced by the <see cref="Scanner"/>.</param>
        public Parser(List<Token> tokens)
        {
            _tokens = tokens;
        }

        /// <summary>
        /// Performs syntax analysis on the tokens provided to this Parser instance and builds an abstract syntax tree.
        /// </summary>
        /// <returns>A <see cref="Node"/> representing the root of the completed abstract syntax tree.</returns>
        public Node BuildTree()
        {
            var topLevelStatements = new List<Node>();
            var isComplete = IsCurrentTokenType(TokenType.EndOfCode);

            // Even code contains no statements, it is still vali
            while (!isComplete)
            {
                // The only valid lines start with a newline or the start of a statement
                if (IsCurrentTokenType(TokenType.Newline))
                {
                    ConsumeToken();
                    isComplete = IsCurrentTokenType(TokenType.EndOfCode);
                    continue;
                }

                // This will throw an exception if the current token is not the start of a statement
                var newStatement = ParseStatement();
                topLevelStatements.Add(newStatement);

                isComplete = IsCurrentTokenType(TokenType.EndOfCode);

                // The last statement does not need a newline. Block statements (def/if) end with a
                // Dedent that already terminates them, so a trailing Newline after them is optional.
                if (isComplete)
                {
                    continue;
                }

                var hasBlock = newStatement is FunctionNode || newStatement is IfStatementNode || newStatement is WhileStatementNode || newStatement is ForStatementNode;

                if (hasBlock)
                {
                    TryConsumeCurrentTokenType(TokenType.Newline);
                    continue;
                }

                ConsumeToken(TokenType.Newline, "Expected newline after statement.");
            }

            ConsumeToken(TokenType.EndOfCode, "Expected end of code.");
            return new TreeRootNode(topLevelStatements);
        }

        #endregion

        #region Statement Methods

        Node ParseBlock()
        {
            ConsumeToken(TokenType.SymbolColon, "Expected ':' after block header.");
            ConsumeToken(TokenType.Newline, "Expected newline after ':'.");

            var indentToken = ConsumeToken(TokenType.Indent, "Expected indented block body.");
            var statements = new List<Node>
            {
                ParseStatement()
            };

            TryConsumeCurrentTokenType(TokenType.Newline);

            while (!IsCurrentTokenType(TokenType.Dedent))
            {
                if (IsCurrentTokenType(TokenType.Newline))
                {
                    ConsumeToken();
                }
                else
                {
                    statements.Add(ParseStatement());
                    TryConsumeCurrentTokenType(TokenType.Newline);
                }
            }

            ConsumeToken(TokenType.Dedent, "Expected dedent to close block.");
            return new BlockNode(statements, indentToken.lineNum);
        }

        Node ParseStatement()
        {
            switch (CurrentToken.type)
            {
                case TokenType.KeywordReturn:
                {
                    return ParseReturnStatement();
                }

                case TokenType.KeywordIf:
                {
                    return ParseIfStatement();
                }

                case TokenType.KeywordDef:
                {
                    return ParseFunctionDefinition();
                }

                case TokenType.KeywordWhile:
                {
                    return ParseWhileStatement();
                }

                case TokenType.KeywordFor:
                {
                    return ParseForStatement();
                }

                case TokenType.KeywordBreak:
                {
                    return ParseBreakStatement();
                }

                case TokenType.KeywordContinue:
                {
                    return ParseContinueStatement();
                }

                case TokenType.KeywordGlobal:
                {
                    return ParseGlobalDeclaration();
                }

                case TokenType.KeywordNonlocal:
                {
                    return ParseNonlocalDeclaration();
                }
            }

            if (!IsPrimaryToken())
            {
                throw new ParserEx("Expected statement.", CurrentToken.lineNum);
            }

            // Parse expression first; if an '=' follows, convert the LHS into the appropriate assignment node.
            // Otherwise, this is a standalone expression statement (result discarded or routed to hook).
            var startLine = CurrentToken.lineNum;
            var lhs = ParseExpression();

            if (!TryConsumeCurrentTokenType(TokenType.SymbolAssign))
            {
                return new ExpressionStatementNode(lhs, startLine);
            }

            var eqLine = PreviousToken.lineNum;
            var rhs = ParseExpression();
            return MakeAssignFromTarget(lhs, rhs, eqLine);
        }

        Node ParseFunctionDefinition()
        {
            var line = CurrentToken.lineNum;

            ConsumeToken(TokenType.KeywordDef, "Expected 'def' keyword.");

            var nameToken = ConsumeToken(TokenType.Identifier, "Expected function name.");

            ConsumeToken(TokenType.SymbolLeftParen, "Expected '(' after function name.");

            var paramList = new List<Node>();

            if (!IsCurrentTokenType(TokenType.SymbolRightParen))
            {
                var paramToken = ConsumeToken(TokenType.Identifier, "Expected parameter name.");
                paramList.Add(new NameNode(paramToken.lexeme, paramToken.lineNum));

                while (TryConsumeCurrentTokenType(TokenType.SymbolComma))
                {
                    paramToken = ConsumeToken(TokenType.Identifier, "Expected parameter name after ','.");
                    paramList.Add(new NameNode(paramToken.lexeme, paramToken.lineNum));
                }
            }

            ConsumeToken(TokenType.SymbolRightParen, "Expected ')' after parameter list.");
            var body = ParseBlock();

            return new FunctionNode(nameToken.lexeme, paramList, body, line);
        }

        Node ParseIfStatement()
        {
            var line = CurrentToken.lineNum;
            ConsumeToken(TokenType.KeywordIf, "Expected 'if' keyword.");

            var expr = ParseExpression();
            var block = ParseBlock();
            var branch = ParseBranchStatement();

            return new IfStatementNode(expr, block, branch, line);
        }

        Node ParseBranchStatement()
        {
            if (IsCurrentTokenType(TokenType.KeywordElif))
            {
                var line = CurrentToken.lineNum;
                ConsumeToken();

                var expr = ParseExpression();
                var block = ParseBlock();
                var branch = ParseBranchStatement();

                return new BranchStatementNode(expr, block, branch, line);
            }

            if (IsCurrentTokenType(TokenType.KeywordElse))
            {
                var line = CurrentToken.lineNum;
                ConsumeToken();

                var block = ParseBlock();
                return new BranchStatementNode(null, block, null, line);
            }

            return null;
        }

        Node ParseWhileStatement()
        {
            var line = CurrentToken.lineNum;
            ConsumeToken(TokenType.KeywordWhile, "Expected 'while' keyword.");

            var expr = ParseExpression();
            var block = ParseBlock();

            return new WhileStatementNode(expr, block, line);
        }

        Node ParseForStatement()
        {
            var line = CurrentToken.lineNum;
            ConsumeToken(TokenType.KeywordFor, "Expected 'for' keyword.");

            var targetToken = ConsumeToken(TokenType.Identifier, "Expected loop variable name after 'for'.");
            var target = new NameNode(targetToken.lexeme, targetToken.lineNum);

            ConsumeToken(TokenType.KeywordIn, "Expected 'in' after loop variable.");

            var iterable = ParseExpression();
            var block = ParseBlock();

            BranchStatementNode elseBranch = null;

            if (IsCurrentTokenType(TokenType.KeywordElse))
            {
                var elseLine = CurrentToken.lineNum;
                ConsumeToken();
                var elseBlock = ParseBlock();
                elseBranch = new BranchStatementNode(expr: null, block: elseBlock, branch: null, line: elseLine);
            }

            return new ForStatementNode(target, iterable, block, elseBranch, line);
        }

        Node ParseBreakStatement()
        {
            var line = CurrentToken.lineNum;
            ConsumeToken(TokenType.KeywordBreak, "Expected 'break' keyword.");
            return new BreakStatementNode(line);
        }

        Node ParseContinueStatement()
        {
            var line = CurrentToken.lineNum;
            ConsumeToken(TokenType.KeywordContinue, "Expected 'continue' keyword.");
            return new ContinueStatementNode(line);
        }

        Node ParseReturnStatement()
        {
            var line = CurrentToken.lineNum;
            ConsumeToken(TokenType.KeywordReturn, "Expected 'return' keyword.");

            // Void functions always return None, and their calls inside expressions will not cause an error
            var expr = IsPrimaryToken() ? ParseExpression() : null;

            return new ReturnStatementNode(expr, line);
        }

        Node ParseGlobalDeclaration()
        {
            var line = CurrentToken.lineNum;
            ConsumeToken(TokenType.KeywordGlobal, "Expected 'global' keyword.");
            var names = ParseDeclarationNameList("global");
            return new GlobalDeclarationNode(names, line);
        }

        Node ParseNonlocalDeclaration()
        {
            var line = CurrentToken.lineNum;
            ConsumeToken(TokenType.KeywordNonlocal, "Expected 'nonlocal' keyword.");
            var names = ParseDeclarationNameList("nonlocal");
            return new NonlocalDeclarationNode(names, line);
        }

        List<string> ParseDeclarationNameList(string keyword)
        {
            var names = new List<string>();
            var firstToken = ConsumeToken(TokenType.Identifier, $"Expected identifier after '{keyword}'.");
            names.Add(firstToken.lexeme);

            while (TryConsumeCurrentTokenType(TokenType.SymbolComma))
            {
                var nameToken = ConsumeToken(TokenType.Identifier, $"Expected identifier after ',' in '{keyword}' declaration.");
                names.Add(nameToken.lexeme);
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

            while (TryConsumeCurrentTokenType(TokenType.KeywordOr))
            {
                var opToken = PreviousToken;
                var rightNode = ParseAnd();
                leftNode = new ExpressionNode(ExpressionOperator.Or, leftNode, rightNode, opToken.lineNum);
            }

            return leftNode;
        }

        Node ParseAnd()
        {
            var leftNode = ParseNot();

            while (TryConsumeCurrentTokenType(TokenType.KeywordAnd))
            {
                var opToken = PreviousToken;
                var rightNode = ParseNot();
                leftNode = new ExpressionNode(ExpressionOperator.And, leftNode, rightNode, opToken.lineNum);
            }

            return leftNode;
        }

        Node ParseNot()
        {
            if (TryConsumeCurrentTokenType(TokenType.KeywordNot))
            {
                var opToken = PreviousToken;
                return new ExpressionNode(ExpressionOperator.Not, ParseNot(), opToken.lineNum);
            }

            return ParseComparison();
        }

        Node ParseComparison()
        {
            var leftNode = ParseBitOr();
            Node result = null;

            while (true)
            {
                ExpressionOperator op;
                int opLine;

                if (TryConsumeCurrentTokenType(
                    TokenType.SymbolEqualTo,
                    TokenType.SymbolNotEqual,
                    TokenType.SymbolLess,
                    TokenType.SymbolGreater,
                    TokenType.SymbolLessEqual,
                    TokenType.SymbolGreaterEqual,
                    TokenType.KeywordIn))
                {
                    op = MapTokenTypeToBinary(PreviousToken.type);
                    opLine = PreviousToken.lineNum;
                }
                else if (IsCurrentTokenType(TokenType.KeywordNot) && PeekNextTokenType(TokenType.KeywordIn))
                {
                    opLine = CurrentToken.lineNum;
                    ConsumeToken();
                    ConsumeToken();
                    op = ExpressionOperator.NotIn;
                }
                else
                {
                    break;
                }

                var rightNode = ParseBitOr();
                Node comparison = new ExpressionNode(op, leftNode, rightNode, opLine);

                if (result == null)
                {
                    result = comparison;
                }
                else
                {
                    result = new ExpressionNode(ExpressionOperator.And, result, comparison, opLine);
                }

                leftNode = rightNode;
            }

            return result ?? leftNode;
        }

        Node ParseBitOr()
        {
            var leftNode = ParseAdd();

            while (TryConsumeCurrentTokenType(TokenType.SymbolPipe))
            {
                var opToken = PreviousToken;
                var rightNode = ParseAdd();
                leftNode = new ExpressionNode(ExpressionOperator.BinaryOr, leftNode, rightNode, opToken.lineNum);
            }

            return leftNode;
        }

        Node ParseAdd()
        {
            var leftNode = ParseTerm();

            while (TryConsumeCurrentTokenType(TokenType.SymbolPlus, TokenType.SymbolMinus))
            {
                var opToken = PreviousToken;
                var rightNode = ParseTerm();
                leftNode = new ExpressionNode(MapTokenTypeToBinary(opToken.type), leftNode, rightNode, opToken.lineNum);
            }

            return leftNode;
        }

        Node ParseTerm()
        {
            var leftNode = ParseFactor();

            while (TryConsumeCurrentTokenType(TokenType.SymbolMultiply, TokenType.SymbolDivide, TokenType.SymbolFloorDivide, TokenType.SymbolPercent))
            {
                var opToken = PreviousToken;
                var rightNode = ParseFactor();
                leftNode = new ExpressionNode(MapTokenTypeToBinary(opToken.type), leftNode, rightNode, opToken.lineNum);
            }

            return leftNode;
        }

        Node ParseFactor()
        {
            if (TryConsumeCurrentTokenType(TokenType.SymbolMinus))
            {
                var opToken = PreviousToken;
                return new ExpressionNode(ExpressionOperator.Negate, ParseFactor(), opToken.lineNum);
            }

            return ParseExponent();
        }

        Node ParseExponent()
        {
            var leftNode = ParsePostfix();

            if (TryConsumeCurrentTokenType(TokenType.SymbolExponent))
            {
                var opToken = PreviousToken;
                var rightNode = ParseFactor();
                return new ExpressionNode(ExpressionOperator.Exponentiate, leftNode, rightNode, opToken.lineNum);
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
                switch (CurrentToken.type)
                {
                    case TokenType.SymbolDot:
                    {
                        node = ParseAttributeAccessTail(node);
                        break;
                    }
                    case TokenType.SymbolLeftBracket:
                    {
                        node = ParseSubscriptTail(node);
                        break;
                    }
                    case TokenType.SymbolLeftParen:
                    {
                        node = ParseInvokeTail(node);
                        break;
                    }
                    default:
                    {
                        isDone = true;
                        break;
                    }
                }
            }
            while (!isDone);

            return node;
        }

        Node ParseAttributeAccessTail(Node target)
        {
            var dotToken = ConsumeToken(TokenType.SymbolDot, "Expected '.'.");
            var nameToken = ConsumeToken(TokenType.Identifier, "Expected attribute name after '.'.");

            return new AttributeAccessNode(target, nameToken.lexeme, dotToken.lineNum);
        }

        Node ParseSubscriptTail(Node target)
        {
            var leftBracket = ConsumeToken(TokenType.SymbolLeftBracket, "Expected '['.");
            var index = ParseSubscriptBody();

            ConsumeToken(TokenType.SymbolRightBracket, "Expected ']' to close subscript.");

            return new SubscriptNode(target, index, leftBracket.lineNum);
        }

        Node ParseSubscriptBody()
        {
            Node start = null;
            Node stop = null;
            Node step = null;

            if (!IsCurrentTokenType(TokenType.SymbolColon))
            {
                var first = ParseExpression();

                if (!IsCurrentTokenType(TokenType.SymbolColon))
                {
                    // Plain index a[i]
                    return first;
                }

                start = first;
            }

            // Positioned on first ':'
            var sliceLine = CurrentToken.lineNum;
            ConsumeToken();

            if (!IsCurrentTokenType(TokenType.SymbolColon) && !IsCurrentTokenType(TokenType.SymbolRightBracket))
            {
                stop = ParseExpression();
            }

            if (TryConsumeCurrentTokenType(TokenType.SymbolColon) && !IsCurrentTokenType(TokenType.SymbolRightBracket))
            {
                step = ParseExpression();
            }

            return new SubscriptSliceNode(start, stop, step, sliceLine);
        }

        Node ParseInvokeTail(Node callNameNode)
        {
            var leftParen = ConsumeToken(TokenType.SymbolLeftParen, "Expected '('.");
            var args = new List<Node>();

            if (!IsCurrentTokenType(TokenType.SymbolRightParen))
            {
                args.Add(ParseExpression());

                while (TryConsumeCurrentTokenType(TokenType.SymbolComma))
                {
                    args.Add(ParseExpression());
                }
            }

            ConsumeToken(TokenType.SymbolRightParen, "Expected ')' after argument list.");
            return new CallNode(callNameNode, args, leftParen.lineNum);
        }

        Node ParseListLiteral()
        {
            var leftBracket = ConsumeToken(TokenType.SymbolLeftBracket, "Expected '['.");
            var elements = new List<Node>();

            if (!IsCurrentTokenType(TokenType.SymbolRightBracket))
            {
                elements.Add(ParseExpression());

                // Allow trailing comma: `[1, 2,]`
                while (TryConsumeCurrentTokenType(TokenType.SymbolComma) && !IsCurrentTokenType(TokenType.SymbolRightBracket))
                {
                    elements.Add(ParseExpression());
                }
            }

            ConsumeToken(TokenType.SymbolRightBracket, "Expected ']' to close list literal.");
            return new ListLiteralNode(elements, leftBracket.lineNum);
        }

        Node ParseDictLiteral()
        {
            var leftCurly = ConsumeToken(TokenType.SymbolLeftCurly, "Expected '{'.");
            var keys = new List<Node>();
            var values = new List<Node>();

            if (!IsCurrentTokenType(TokenType.SymbolRightCurly))
            {
                ParseDictEntry(keys, values);

                while (TryConsumeCurrentTokenType(TokenType.SymbolComma) && !IsCurrentTokenType(TokenType.SymbolRightCurly))
                {
                    ParseDictEntry(keys, values);
                }
            }

            ConsumeToken(TokenType.SymbolRightCurly, "Expected '}' to close dict literal.");
            return new ListDictNode(keys, values, leftCurly.lineNum);
        }

        void ParseDictEntry(List<Node> keys, List<Node> values)
        {
            var key = ParseExpression();
            ConsumeToken(TokenType.SymbolColon, "Expected ':' between dict key and value.");
            var value = ParseExpression();
            keys.Add(key);
            values.Add(value);
        }

        Node ParsePrimary()
        {
            // Note: After adding a new primary token type, remember to update IsPrimaryTokenType() as well. Not doing
            //       so will cause IsPrimaryTokenType() to return false for the new TokenType, which will break certain
            //       statements that behaviors rely on knowing whether an expression is present or not (e.g. return statements).
            switch (CurrentToken.type)
            {
                case TokenType.Identifier:
                {
                    var idToken = CurrentToken;
                    ConsumeToken();
                    return new NameNode(idToken.lexeme, idToken.lineNum);
                }

                case TokenType.LiteralInt:
                case TokenType.LiteralFloat:
                case TokenType.LiteralStr:
                {
                    var numToken = CurrentToken;
                    ConsumeToken();
                    return new LiteralNode(numToken.literal, numToken.lineNum);
                }

                case TokenType.KeywordNone:
                {
                    ConsumeToken(TokenType.KeywordNone);
                    return new LiteralNode(value: null, PreviousToken.lineNum);
                }

                case TokenType.KeywordTrue:
                {
                    ConsumeToken(TokenType.KeywordTrue);
                    return new LiteralNode(value: true, PreviousToken.lineNum);
                }

                case TokenType.KeywordFalse:
                {
                    ConsumeToken(TokenType.KeywordFalse);
                    return new LiteralNode(value: false, PreviousToken.lineNum);
                }

                case TokenType.SymbolLeftParen:
                {
                    ConsumeToken();
                    var inner = ParseExpression();
                    ConsumeToken(TokenType.SymbolRightParen);
                    return inner;
                }

                case TokenType.SymbolLeftBracket:
                {
                    return ParseListLiteral();
                }

                case TokenType.SymbolLeftCurly:
                {
                    return ParseDictLiteral();
                }

                default:
                {
                    throw new ParserEx("Expected expression.", CurrentToken.lineNum);
                }
            }
        }

        #endregion

        #region Token Helpers

        void ConsumeToken()
        {
            if (CurrentToken.type != TokenType.EndOfCode)
            {
                _tokenIdx++;
            }
        }

        bool IsCurrentTokenType(TokenType type)
        {
            return CurrentToken.type == type;
        }

        bool PeekNextTokenType(TokenType type, int offset = 1)
        {
            var nextIndex = _tokenIdx + offset;

            // This method will never be called when the current token is EndOfCode, so we don't need to check for out-of-range.
            return _tokens[nextIndex].type == type;
        }

        bool TryConsumeCurrentTokenType(params TokenType[] types)
        {
            foreach (var type in types)
            {
                if (IsCurrentTokenType(type))
                {
                    ConsumeToken();
                    return true;
                }
            }

            return false;
        }

        Token ConsumeToken(TokenType type, string message = "")
        {
            if (!IsCurrentTokenType(type))
            {
                throw new ParserEx(message, CurrentToken.lineNum);
            }

            var token = CurrentToken;
            ConsumeToken();
            return token;
        }

        bool IsPrimaryToken()
        {
            switch (CurrentToken.type)
            {
                case TokenType.Identifier: 
                case TokenType.LiteralInt:
                case TokenType.LiteralFloat:
                case TokenType.LiteralStr:
                case TokenType.KeywordNone:
                case TokenType.KeywordTrue:
                case TokenType.KeywordFalse:
                case TokenType.KeywordNot:
                case TokenType.SymbolLeftParen:
                case TokenType.SymbolLeftBracket:
                case TokenType.SymbolLeftCurly:
                {
                    return true;
                }
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
                {
                    return new VariableAssignStatementNode(nameNode.Name, value, line);

                }
                case SubscriptNode subscriptNode:
                {
                    return new SubscriptAssignNode(subscriptNode.Target, subscriptNode.Index, value, line);
                }
                case AttributeAccessNode attrNode:
                {
                    return new AttributeAssignNode(attrNode.Target, attrNode.AttributeName, value, line);
                }
            }

            throw new ParserEx("Invalid assignment target.", line);
        }

        static ExpressionOperator MapTokenTypeToBinary(TokenType type)
        {
            switch (type)
            {
                case TokenType.SymbolPlus:
                {
                    return ExpressionOperator.Add;
                }

                case TokenType.SymbolMinus:
                {
                    return ExpressionOperator.Subtract;
                }

                case TokenType.SymbolMultiply:
                {
                    return ExpressionOperator.Multiply;
                }

                case TokenType.SymbolDivide:
                {
                    return ExpressionOperator.Divide;
                }

                case TokenType.SymbolPercent:
                {
                    return ExpressionOperator.Modulus;
                }

                case TokenType.SymbolExponent:
                {
                    return ExpressionOperator.Exponentiate;
                }

                case TokenType.SymbolFloorDivide:
                {
                    return ExpressionOperator.FloorDivide;
                }

                case TokenType.SymbolEqualTo:
                {
                    return ExpressionOperator.Equal;
                }

                case TokenType.SymbolNotEqual:
                {
                    return ExpressionOperator.NotEqual;
                }

                case TokenType.SymbolLess:
                {
                    return ExpressionOperator.Less;
                }

                case TokenType.SymbolGreater:
                {
                    return ExpressionOperator.Greater;
                }

                case TokenType.SymbolLessEqual:
                {
                    return ExpressionOperator.LessEqual;
                }

                case TokenType.SymbolGreaterEqual:
                {
                    return ExpressionOperator.GreaterEqual;
                }

                case TokenType.KeywordAnd:
                {
                    return ExpressionOperator.And;
                }

                case TokenType.KeywordOr:
                {
                    return ExpressionOperator.Or;
                }

                case TokenType.SymbolPipe:
                {
                    return ExpressionOperator.BinaryOr;
                }

                case TokenType.KeywordIn:
                {
                    return ExpressionOperator.In;
                }

                default:
                {
                    throw new InvalidOperationException();
                }
            }
        }

        #endregion
    }
}
