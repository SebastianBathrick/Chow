using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Chow.Code;
using Chow.Interpreter.Exceptions;
using Chow.Tokens;

namespace Chow.Interpreter.Lexing
    {
        /// <summary>
        /// <para>
        /// Instances facilitate the first phase of the interpreter, lexical analysis/scanning. The
        /// client provides source code via an argument passed to an instance's constructor.
        /// </para>
        /// <para>
        /// To begin lexical analysis, the client must call the <see cref="TokenizeSourceCode"/>
        /// method, which tokenizes the source code and returns a list of <see cref="Token"/>s.
        /// After <see cref="TokenizeSourceCode"/>, the Scanner instance will be considered dirty,
        /// and cannot be used again.
        /// </para>
        /// </summary>
        sealed class Scanner
        {
            const int SingleIndentSize = 4;
    
            readonly string _sourceCode;
            readonly ITokenStream _tokenStream;
    
            readonly Stack<int> _indentLevels;
            readonly Stack<char> _openingBrackets;
    
            int _charIdx;
            int _lineNum;
            bool _isLineBegin;
    
            /// <summary>Initializes a new Scanner instance using Chow source code.</summary>
            /// <param name="sourceCode">Null or string containing raw Chow source code or whitespace.</param>
            /// <param name="tokenStream">Stream instance to inject that will store analyzed tokens.</param>
            public Scanner(string sourceCode, ITokenStream tokenStream)
            {
                _sourceCode = sourceCode;
                _tokenStream = tokenStream;
                _charIdx = 0;
                _lineNum = 1;
                _indentLevels = new Stack<int>();
                _openingBrackets = new Stack<char>();
                _isLineBegin = true;
                _indentLevels.Push(0);
            }
    
            /// <summary>
            /// Scans the source code string provided during this instance's initialization and
            /// generates a list of tokens.
            /// </summary>
            /// <returns>A list of tokens representing the scanned source code in the order they appear.</returns>
            public ITokenStream TokenizeSourceCode()
            {
                // If source code is null, emit end-of-code token, so it can be treated as if it were 
                // an empty string or whitespace
                if (_sourceCode == null)
                {
                    AddNewToken(TokenType.EndOfCode, string.Empty, _lineNum);
                    return _tokenStream;
                }
    
                // Skip to the first line that does not start with whitespace, a comment, or newline character
                SkipToFirstLexeme();
    
                while (IsCharToScan)
                {
                    RunScanIteration();
                }
    
                if (_openingBrackets.Count > 0)
                {
                    throw new ScannerException("Bracket(s) never closed in source code", _lineNum);
                }
    
                // BinaryAdd dedent tokens for each block nested within the top-level to mark their end
                AddLastDedentsTokens();
                AddNewToken(TokenType.EndOfCode, string.Empty, _lineNum);
    
                return _tokenStream;
            }
    
            void RunScanIteration()
            {
                if (_isLineBegin)
                {
                    // If there are opening bracket(s) then indentation and newlines will be ignored
                    if (_openingBrackets.Count == 0)
                    {
                        ScanIndentToken();
                    }
    
                    if (!IsCharToScan)
                    {
                        return;
                    }
                }
    
                // TODO: Refactor to be in-order of: Most common case -> Least common case
                if (CharMap.IsOfType(CurrentChar, CharType.FStringPrefix) 
                    && CharMap.IsOfType(PeekNextChar(), CharType.Quote))
                {
                    MoveToNextChar(); // skip f/F
                    ScanFStringToken();
                }
                else if (CharMap.IsOfType(CurrentChar, CharType.IdentifierPrefix))
                {
                    ScanNameToken();
                }
                else if (CharMap.IsOfType(CurrentChar, CharType.Newline))
                {
                    ScanNewlineToken();
                }
                else if (CharMap.IsOfType(CurrentChar, CharType.Digit)
                         || CurrentChar == '.' && CharMap.IsOfType(PeekNextChar(), CharType.Digit))
                {
                    ScanNumericToken();
                }
                else if (CharMap.IsOfType(CurrentChar, CharType.Quote))
                {
                    ScanStringToken();
                }
                else if (CharMap.IsOfType(CurrentChar, CharType.Indent))
                {
                    MoveToNextChar();
                }
                else if (CharMap.IsOfType(CurrentChar, CharType.CommentPrefix))
                {
                    SkipRemainingLineChars();
                }
                else if (!TryScanSymbolToken())
                {
                    throw new ScannerException($"Unexpected character '{CurrentChar}'.", _lineNum);
                }
            }
    
            #region Newline & Indentation Token Scan Methods
    
            void ScanNameToken()
            {
                var startIdx = _charIdx;
    
                while (IsCharToScan && CharMap.IsOfType(CurrentChar, CharType.IdentifierSuffix))
                {
                    MoveToNextChar();
                }
    
                var lexeme = _sourceCode.Substring(startIdx, _charIdx - startIdx);

                AddNewToken(Keywords.Contains(lexeme) 
                        ? Keywords.GetTokenType(lexeme) 
                        : TokenType.Name, lexeme, _lineNum);
            }
    
            void ScanNewlineToken()
            {
                // Newlines are ignored when inside brackets
                if (_openingBrackets.Count == 0)
                {
                    // Use a newline for the lexeme for clean debug information
                    AddNewToken(TokenType.Newline, "\n", _lineNum);
                }
    
                MovePastNewline();
            }
    
            void ScanIndentToken()
            {
                var indentColumn = ScanIndentColumn();
    
                if (!IsCharToScan)
                {
                    return;
                }
    
                if (CharMap.IsOfType(CurrentChar, CharType.CommentPrefix))
                {
                    return;
                }
    
                if (CharMap.IsOfType(CurrentChar, CharType.Newline))
                {
                    return;
                }
    
                CreateIndentTokens(indentColumn);
                _isLineBegin = false;
            }
    
            int ScanIndentColumn()
            {
                var indentColumn = 0;
    
                while (IsCharToScan && CharMap.IsOfType(CurrentChar, CharType.FormFeed))
                {
                    MoveToNextChar();
                }
    
                while (IsCharToScan && CharMap.IsOfType(CurrentChar, CharType.Indent))
                {
                    if (CurrentChar == '\t')
                    {
                        // Tab rounds column up to the next multiple of 4 (Python rule)
                        indentColumn = (indentColumn / SingleIndentSize + 1) * SingleIndentSize;
                    }
                    else
                    {
                        indentColumn++;
                    }
    
                    MoveToNextChar();
                }
    
                return indentColumn;
            }
    
            void CreateIndentTokens(int indentLvl)
            {
                var prevIndentLvl = _indentLevels.Peek();
    
                if (indentLvl > prevIndentLvl)
                {
                    _indentLevels.Push(indentLvl);
                    AddNewToken(TokenType.Indent, " ", _lineNum);
                    return;
                }
    
                if (indentLvl == prevIndentLvl)
                {
                    return;
                }
    
                while (_indentLevels.Peek() > indentLvl)
                {
                    _indentLevels.Pop();
                    AddNewToken(TokenType.Dedent, string.Empty, _lineNum);
                }
    
                if (_indentLevels.Peek() != indentLvl)
                {
                    throw new ScannerException("Inconsistent dedent.", _lineNum);
                }
            }
    
            void AddLastDedentsTokens()
            {
                while (_indentLevels.Count > 1)
                {
                    _indentLevels.Pop();
                    AddNewToken(TokenType.Dedent, string.Empty, _lineNum);
                }
            }
    
            #endregion
    
            #region Lexeme-Dependent Token Scan Methods
    
            bool TryScanSymbolToken()
            {
                TokenType tokenType;
    
                switch (CurrentChar)
                {
                    case ',':
                        tokenType = TokenType.SymbolComma;
                        break;
                    case '.':
                        tokenType = TokenType.SymbolDot;
                        break;
                    case ':':
                        tokenType = TokenType.SymbolColon;
                        break;
                    case '+':
                        tokenType = TokenType.SymbolPlus;
                        break;
                    case '-':
                        tokenType = TokenType.SymbolMinus;
                        break;
                    case '*':
                        if (TryScanCompoundOp('*', TokenType.SymbolExponent, "**"))
                        {
                            return true;
                        }
    
                        tokenType = TokenType.SymbolMultiply;
                        break;
    
                    case '/':
                        if (TryScanCompoundOp('/', TokenType.SymbolFloorDivide, "//"))
                        {
                            return true;
                        }
    
                        tokenType = TokenType.SymbolDivide;
                        break;
                    case '%':
                        tokenType = TokenType.SymbolPercent;
                        break;
    
                    case '|':
                        tokenType = TokenType.SymbolPipe;
                        break;
                    case '!':
                        return TryScanCompoundOp('=', TokenType.SymbolNotEqual, "!=");
    
                    case '=':
                        if (TryScanCompoundOp('=', TokenType.SymbolEqualTo, "=="))
                        {
                            return true;
                        }
    
                        tokenType = TokenType.SymbolAssign;
                        break;
                    case '>':
                        if (TryScanCompoundOp('=', TokenType.SymbolGreaterEqual, ">="))
                        {
                            return true;
                        }
    
                        tokenType = TokenType.SymbolGreater;
                        break;
                    case '<':
                        if (TryScanCompoundOp('=', TokenType.SymbolLessEqual, "<="))
                        {
                            return true;
                        }
    
                        tokenType = TokenType.SymbolLess;
                        break;
    
                    // Indentation and line-break rules are not enforced (for lists and dictionary declarations)
                    case '[':
                        tokenType = TokenType.SymbolLeftBracket;
                        PushOpeningBracket('[');
                        break;
                    case ']':
                        tokenType = TokenType.SymbolRightBracket;
                        PopClosingBracket(']', '[');
                        break;
                    case '{':
                        tokenType = TokenType.SymbolLeftCurly;
                        PushOpeningBracket('{');
                        break;
                    case '}':
                        tokenType = TokenType.SymbolRightCurly;
                        PopClosingBracket('}', '{');
                        break;
                    case '(':
                        tokenType = TokenType.SymbolLeftParen;
                        PushOpeningBracket('(');
                        break;
                    case ')':
                        tokenType = TokenType.SymbolRightParen;
                        PopClosingBracket(')', '(');
                        break;
                    default:
                        return false;
                }
    
                var lexeme = CurrentChar.ToString();
                MoveToNextChar();
                AddNewToken(tokenType, lexeme, _lineNum);
                return true;
            }
    
            bool TryScanCompoundOp(char secondChar, TokenType compoundType, string compoundStrRep)
            {
                if (PeekNextChar() != secondChar)
                {
                    return false;
                }
    
                MoveToNextChar();
                MoveToNextChar();
                AddNewToken(compoundType, compoundStrRep, _lineNum);
                return true;
            }
    
            void ScanNumericToken()
            {
                var startIdx = _charIdx;
    
                var isFloat = CurrentChar == '.';
    
                if (!isFloat)
                {
                    // Move past digits before any decimal point (if any)
                    do
                    {
                        MoveToNextChar();
                    }
                    while (IsCharToScan && CharMap.IsOfType(CurrentChar, CharType.Digit));
                }
    
                // If there is a decimal point, move past it and any following digits
                isFloat = isFloat || IsCharToScan && CurrentChar == '.';
    
                if (isFloat)
                {
                    ScanFloatTrailingDigits();
                }
    
                var len = _charIdx - startIdx;
                var lexeme = _sourceCode.Substring(startIdx, len);
                var numTokenType = isFloat ? TokenType.LiteralFloat : TokenType.LiteralInt;
                var literal = ParseNumericLiteral(isFloat, lexeme, numTokenType);
    
                AddNewToken(numTokenType, lexeme, _lineNum, literal);
            }
    
            void ScanFloatTrailingDigits()
            {
                MoveToNextChar();
    
                while (IsCharToScan && CharMap.IsOfType(CurrentChar, CharType.Digit))
                {
                    MoveToNextChar();
                }
            }
    
            static object ParseNumericLiteral(bool isFloat, string lexeme, TokenType numTokenType)
            {
                object literal;
    
                try
                {
                    if (isFloat)
                    {
                        literal = double.Parse(lexeme, CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        literal = long.Parse(lexeme, CultureInfo.InvariantCulture);
                    }
                }
                catch (OverflowException)
                {
                    // TODO: Refactor to use Python-appropriate exception.
                    throw new OverflowException(
                        $"{numTokenType} literal value out of range & parsing failed. "
                        + $"Literal Value: {lexeme}");
                }
                catch (FormatException)
                {
                    // This should never occur, and if it does, the scanner logic is incorrect
                    throw new InvalidOperationException();
                }
    
                return literal;
            }
    
            void ScanStringToken()
            {
                var quoteChar = CurrentChar;
                var startIdx = _charIdx;
                MoveToNextChar();
    
                var literal = ScanStringContent(quoteChar);
    
                if (!IsCharToScan)
                {
                    throw new ScannerException("Unterminated string literal", _lineNum);
                }
    
                MoveToNextChar();
    
                var lexeme = _sourceCode.Substring(startIdx, _charIdx - startIdx);
                AddNewToken(TokenType.LiteralStr, lexeme, _lineNum, literal);
            }
    
            string ScanStringContent(char quoteChar)
            {
                var builder = new StringBuilder();
    
                while (IsCharToScan && CurrentChar != quoteChar)
                {
                    if (CharMap.IsOfType(CurrentChar, CharType.Newline))
                    {
                        throw new ScannerException("Unterminated string literal", _lineNum);
                    }
    
                    if (CurrentChar == '\\')
                    {
                        MoveToNextChar();
    
                        if (!IsCharToScan)
                        {
                            throw new ScannerException("Unterminated string literal", _lineNum);
                        }
    
                        builder.Append(DecodeEscape(CurrentChar));
                    }
                    else
                    {
                        builder.Append(CurrentChar);
                    }
    
                    MoveToNextChar();
                }
    
                return builder.ToString();
            }
    
            char DecodeEscape(char c)
            {
                return !CharMap.TryDecodeEscape(c, out var result) 
                    ? throw new ScannerException($"Unknown escape sequence '\\{c}'", _lineNum) 
                    : result;

            }
    
            // THIS IS A TEMPORARY SOLUTION! DO NOT ADD ANY ADDITIONAL LOGIC!
            void ScanFStringToken()
            {
                var startIdx = _charIdx - 1; // position of f/F
                var quoteChar = CurrentChar;
                MoveToNextChar(); // skip opening quote
    
                var stringParts = new List<string>();
                var exprParts = new List<string>();
                var currentPart = new StringBuilder();
    
                while (IsCharToScan && CurrentChar != quoteChar)
                {
                    if (CharMap.IsOfType(CurrentChar, CharType.Newline))
                    {
                        throw new ScannerException("Unterminated f-string literal", _lineNum);
                    }
    
                    switch (CurrentChar)
                    {
                        case '{' when PeekNextChar() == '{':
                            currentPart.Append('{');
                            MoveToNextChar();
                            MoveToNextChar();
                            continue;
                        
                        case '{':
                            MoveToNextChar(); // skip opening {
                            stringParts.Add(currentPart.ToString());
                            currentPart.Clear();
                            exprParts.Add(ScanFStringSlot());
                            continue;
                        
                        case '}' when PeekNextChar() == '}':
                            currentPart.Append('}');
                            MoveToNextChar();
                            MoveToNextChar();
                            continue;
                        
                        case '\\':
                            MoveToNextChar();
    
                            if (!IsCharToScan)
                            {
                                throw new ScannerException(
                                    "Unterminated f-string literal", _lineNum);
                            }
    
                            currentPart.Append(DecodeEscape(CurrentChar));
                            MoveToNextChar();
                            continue;
                        
                        case '}':
                            throw new ScannerException(
                                "Single '}' is not allowed in f-string", _lineNum);
                        
                        default:
                            currentPart.Append(CurrentChar);
                            MoveToNextChar();
                            break;
                    }
    
                }
    
                if (!IsCharToScan)
                {
                    throw new ScannerException("Unterminated f-string literal", _lineNum);
                }
    
                stringParts.Add(currentPart.ToString());
                MoveToNextChar(); // skip closing quote
    
                var lexeme = _sourceCode.Substring(startIdx, _charIdx - startIdx);
                var payload = new FStringTokenPayload(stringParts, exprParts);
                AddNewToken(TokenType.LiteralFString, lexeme, _lineNum, payload);
            }
    
            string ScanFStringSlot()
            {
                var slotSource = new StringBuilder();
                var depth = 1;
    
                while (IsCharToScan && depth > 0)
                {
                    if (CharMap.IsOfType(CurrentChar, CharType.Newline))
                    {
                        throw new ScannerException("Unterminated f-string expression", _lineNum);
                    }
    
                    var c = CurrentChar;
    
                    if (c == '{')
                    {
                        depth++;
                        slotSource.Append(c);
                        MoveToNextChar();
                        continue;
                    }
    
                    if (c == '}')
                    {
                        depth--;
    
                        if (depth == 0)
                        {
                            break;
                        }
    
                        slotSource.Append(c);
                        MoveToNextChar();
                        continue;
                    }
    
                    if (c == '\'' || c == '"')
                    {
                        slotSource.Append(c);
                        MoveToNextChar();
    
                        while (IsCharToScan && CurrentChar != c)
                        {
                            if (CharMap.IsOfType(CurrentChar, CharType.Newline))
                            {
                                throw new ScannerException(
                                    "Unterminated string in f-string expression", _lineNum);
                            }
    
                            if (CurrentChar == '\\')
                            {
                                slotSource.Append(CurrentChar);
                                MoveToNextChar();
    
                                if (!IsCharToScan)
                                {
                                    throw new ScannerException(
                                        "Unterminated string in f-string expression", _lineNum);
                                }
                            }
    
                            slotSource.Append(CurrentChar);
                            MoveToNextChar();
                        }
    
                        if (!IsCharToScan)
                        {
                            throw new ScannerException(
                                "Unterminated string in f-string expression", _lineNum);
                        }
    
                        slotSource.Append(CurrentChar); // closing quote
                        MoveToNextChar();
                        continue;
                    }
    
                    slotSource.Append(c);
                    MoveToNextChar();
                }
    
                if (!IsCharToScan)
                {
                    throw new ScannerException("Unterminated f-string expression", _lineNum);
                }
    
                MoveToNextChar(); // skip closing }
    
                var result = slotSource.ToString().Trim();
    
                return result.Length == 0 
                    ? throw new ScannerException("f-string: empty expression not allowed", _lineNum) 
                    : result;
    
            }
    
            #endregion
    
            #region Char Pointer Methods
    
            bool IsCharToScan => _charIdx < _sourceCode.Length;
    
            char CurrentChar => _sourceCode[_charIdx];
    
            void MoveToNextChar()
            {
                _charIdx++;
                _isLineBegin = false;
            }
    
            char PeekNextChar()
            {
                var nextIndex = _charIdx + 1;
                return nextIndex < _sourceCode.Length ? _sourceCode[nextIndex] : '\0';
            }
    
            #endregion
    
            #region Helper Methods
    
            void MovePastNewline()
            {
                switch (CurrentChar)
                {
                    case '\n':
                        // Unix/Linux/macOS newline
                        MoveToNextChar();
                        break;
    
                    case '\r':
                        // Older Mac newline (if not followed by \n)
                        MoveToNextChar();
    
                        if (IsCharToScan && CurrentChar == '\n')
                        {
                            // Windows/MS-DOS newline
                            MoveToNextChar();
                        }
    
                        break;
                }
    
                _isLineBegin = true;
                _lineNum++;
            }
    
            void SkipToFirstLexeme()
            {
                while (IsCharToScan)
                {
                    if (CharMap.IsOfType(CurrentChar, CharType.Indent))
                    {
                        MoveToNextChar();
                    }
                    else if (CharMap.IsOfType(CurrentChar, CharType.Newline))
                    {
                        MovePastNewline();
                    }
                    else if (CharMap.IsOfType(CurrentChar, CharType.CommentPrefix))
                    {
                        SkipRemainingLineChars();
                    }
                    else if (_isLineBegin)
                    {
                        return;
                    }
                    else
                    {
                        throw new ScannerException("Unexpected indentation.", _lineNum);
                    }
                }
            }
    
            void SkipRemainingLineChars()
            {
                while (IsCharToScan && !CharMap.IsOfType(CurrentChar, CharType.Newline))
                {
                    MoveToNextChar();
                }
            }
    
            void PushOpeningBracket(char bracket)
            {
                _openingBrackets.Push(bracket);
            }
    
            void PopClosingBracket(char closingChar, char expectedOpening)
            {
                if (_openingBrackets.Count == 0 || _openingBrackets.Pop() != expectedOpening)
                {
                    throw new ScannerException($"Unexpected '{closingChar}'", _lineNum);
                }
            }
    
            void AddNewToken(TokenType type, string lexeme, int lineNum, object literal = null)
            {
                // Refactor to use ITokenStream instead
                _tokenStream.Add(new Token(type, lexeme, lineNum, literal));
            }
    
            #endregion
        }
    }
