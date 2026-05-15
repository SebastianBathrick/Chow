using Chow.Interpreter.Exceptions;
using Chow.Interpreter.Tokens;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Chow.Interpreter
{
    /// <summary>
    /// Instances facilitate the first phase of the interpreter, lexical analysis/scanning. The client provides source
    /// code via an argument passed to an instance's constructor.
    /// <para>
    /// To begin lexical analysis, the client must call the <see cref="ScanTokens"/> method, which tokenizes the source
    /// code and returns a list of <see cref="Token"/>s. After <see cref="ScanTokens"/> returns its value, the client
    /// should discard the Scanner instance, because it will be considered dirty.
    /// </para>
    /// </summary>
    sealed class Scanner
    {
        #region Fields & Consts

        const int TAB_SIZE = 4;

        readonly List<Token> _tokenList;
        readonly Stack<int> _indentLevels;
        readonly Stack<char> _openingBrackets;
        readonly string _sourceCode;

        int _charIdx;
        int _lineNum;
        bool _isLineBegin;

        char CurrChar => _sourceCode[_charIdx];

        #endregion

        #region Constructor & Primary Methods

        /// <summary>
        /// Initializes a new Scanner instance using Chow source code.
        /// </summary>
        /// <param name="sourceCode">Null or string containing raw Chow source code or whitespace.</param>
        public Scanner(string sourceCode)
        {
            _sourceCode = sourceCode;
            _tokenList = new List<Token>();
            _charIdx = 0;
            _lineNum = 1;
            _indentLevels = new Stack<int>();
            _openingBrackets = new Stack<char>();
            _isLineBegin = true;
            _indentLevels.Push(0);
        }

        /// <summary>
        /// Scans the source code string provided during this instance's initialization, and generates a list of tokens.
        /// </summary>
        /// <returns>A list of tokens representing the scanned source code in the order they appear.</returns>
        public List<Token> ScanTokens()
        {
            // If source code is null, emit end of code token, so it can be treated as if it were an empty string or whitespace
            if (_sourceCode == null)
            {
                AddEndOfCodeToken();
                return _tokenList;
            }

            // Skip to the first line that does not start with whitespace, a comment, or newline character
            SkipToFirstLexeme();

            while (IsCharToScan())
            {
                RunScanIteration();
            }

            if (_openingBrackets.Count > 0)
            {
                throw new ScannerEx("Bracket(s) never closed in source code", _lineNum);
            }

            // Add dedent tokens for each block nested within the top-level to mark their end
            AddLastDedentsTokens();
            AddEndOfCodeToken();

            return _tokenList;
        }

        void RunScanIteration()
        {
            if (_isLineBegin)
            {
                if (IsLineAndIndentLogicEnabled())
                {
                    ScanIndentToken();
                }

                if (!IsCharToScan())
                {
                    return;
                }
            }

            if (IsNameLeadingChar(CurrChar))
            {
                ScanNameToken();
            }
            else if (IsNewlineChar(CurrChar))
            {
                ScanNewlineToken();
            }
            else if (IsDigitChar(CurrChar))
            {
                ScanNumericToken();
            }
            else if (IsQuoteChar(CurrChar))
            {
                ScanStringToken();
            }
            else if (IsIndentChar(CurrChar))
            {
                MoveToNextChar();
            }
            else if (IsCommentPrefix(CurrChar))
            {
                SkipRemainingLineChars();
            }
            else if (!TryScanSymbolToken())
            {
                throw new ScannerEx($"Unexpected character '{CurrChar}'.", _lineNum);
            }
        }


        #endregion

        #region Newline & Indentation Token Scan Methods

        void ScanNameToken()
        {
            var startIdx = _charIdx;

            while (IsCharToScan() && IsNameTrailChar(CurrChar))
            {
                MoveToNextChar();
            }

            var lexeme = _sourceCode.Substring(startIdx, _charIdx - startIdx);

            if (ReservedKeywords.Contains(lexeme))
            {
                AddNewToken(ReservedKeywords.GetTokenType(lexeme), lexeme, _lineNum);
                return;
            }

            AddNewToken(TokenType.Identifier, lexeme, _lineNum);
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

            if (!IsCharToScan() || IsNewlineChar(CurrChar) || IsCommentPrefix(CurrChar))
            {
                return;
            }

            CreateIndentTokens(indentColumn);
            _isLineBegin = false;
        }

        int ScanIndentColumn()
        {
            var indentColumn = 0;

            while (IsCharToScan() && IsFormFeedChar(CurrChar))
            {
                MoveToNextChar();
            }

            while (IsCharToScan() && IsIndentChar(CurrChar))
            {
                if (CurrChar == '\t')
                {
                    // Tab rounds column up to the next multiple of TAB_SIZE (Python rule)
                    indentColumn = (indentColumn / TAB_SIZE + 1) * TAB_SIZE;
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
                throw new ScannerEx("Inconsistent dedent.", _lineNum);
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

        // TODO: Refactor all project switches to use curly braces
        bool TryScanSymbolToken()
        {
            TokenType tokenType;

            switch (CurrChar)
            {
                case ',':
                    {
                        tokenType = TokenType.SymbolComma;
                        break;
                    }


                case '.':
                    {
                        tokenType = TokenType.SymbolDot;
                        break;
                    }

                case ':':
                    {
                        tokenType = TokenType.SymbolColon;
                        break;
                    }

                case '+':
                    {
                        tokenType = TokenType.SymbolPlus;
                        break;
                    }

                case '-':
                    {
                        tokenType = TokenType.SymbolMinus;
                        break;
                    }

                case '*':
                    {
                        if (TryScanCompoundOp('*', TokenType.SymbolExponent, "**"))
                        {
                            return true;
                        }

                        tokenType = TokenType.SymbolMultiply;
                        break;
                    }

                case '/':
                    {
                        if (TryScanCompoundOp('/', TokenType.SymbolFloorDivide, "//"))
                        {
                            return true;
                        }

                        tokenType = TokenType.SymbolDivide;
                        break;
                    }

                case '%':
                    {
                        tokenType = TokenType.SymbolPercent;
                        break;
                    }

                case '|':
                    {
                        tokenType = TokenType.SymbolPipe;
                        break;
                    }

                case '!':
                    {
                        return TryScanCompoundOp('=', TokenType.SymbolNotEqual, "!=");
                    }

                case '=':
                    {
                        if (TryScanCompoundOp('=', TokenType.SymbolEqualTo, "=="))
                        {
                            return true;
                        }

                        tokenType = TokenType.SymbolAssign;
                        break;
                    }

                case '>':
                    {
                        if (TryScanCompoundOp('=', TokenType.SymbolGreaterEqual, ">="))
                        {
                            return true;
                        }

                        tokenType = TokenType.SymbolGreater;
                        break;
                    }
                case '<':
                    {
                        if (TryScanCompoundOp('=', TokenType.SymbolLessEqual, "<="))
                        {
                            return true;
                        }

                        tokenType = TokenType.SymbolLess;
                        break;
                    }

                // Indentation and line-break rules are not enforced (for lists and dictionary declarations)
                case '[':
                    {
                        tokenType = TokenType.SymbolLeftBracket;
                        _openingBrackets.Push('[');
                        break;
                    }

                // TODO: Refactor to reduce repeated closing bracket logic
                case ']':
                    {
                        tokenType = TokenType.SymbolRightBracket;

                        if (_openingBrackets.Count == 0 || _openingBrackets.Pop() != '[')
                        {
                            throw new ScannerEx("Unexpected ']'", _lineNum);
                        }
                        break;
                    }

                case '{':
                    {
                        tokenType = TokenType.SymbolLeftCurly;
                        _openingBrackets.Push('{');
                        break;
                    }

                case '}':
                    {
                        tokenType = TokenType.SymbolRightCurly;

                        if (_openingBrackets.Count == 0 || _openingBrackets.Pop() != '{')
                        {
                            throw new ScannerEx("Unexpected '}'", _lineNum);
                        }
                        break;
                    }

                case '(':
                    {
                        tokenType = TokenType.SymbolLeftParen;
                        _openingBrackets.Push('(');
                        break;
                    }

                case ')':
                    {
                        tokenType = TokenType.SymbolRightParen;

                        if (_openingBrackets.Count == 0 || _openingBrackets.Pop() != '(')
                        {
                            throw new ScannerEx("Unexpected ')'", _lineNum);
                        }
                        break;
                    }

                default:
                    {
                        return false;
                    }
            }

            var lexeme = CurrChar.ToString();
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

            // Move past digits before any decimal point (if any)
            MoveToNextChar();

            while (IsCharToScan() && IsDigitChar(CurrChar))
            {
                MoveToNextChar();
            }

            // If there is a decimal point, move past it and any following digits
            var isFloat = IsCharToScan() && CurrChar == '.';

            if (isFloat)
            {
                MoveToNextChar();

                while (IsCharToScan() && IsDigitChar(CurrChar))
                {
                    MoveToNextChar();
                }
            }

            var len = _charIdx - startIdx;
            var lexeme = _sourceCode.Substring(startIdx, len);
            var numTokenType = isFloat ? TokenType.LiteralFloat : TokenType.LiteralInt;
            object literal;

            literal = ParseNumericLiteral(isFloat, lexeme, numTokenType);

            AddNewToken(numTokenType, lexeme, _lineNum, literal);
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
                throw new OverflowException($"{numTokenType} literal value out of range & parsing failed. Literal Value: {lexeme}");
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
            var quoteChar = CurrChar;
            var startIdx = _charIdx;
            MoveToNextChar();

            var contentStartIdx = _charIdx;

            while (IsCharToScan() && CurrChar != quoteChar)
            {
                if (IsNewlineChar(CurrChar))
                {
                    throw new ScannerEx("Unterminated string literal", _lineNum);
                }

                MoveToNextChar();
            }

            if (!IsCharToScan())
            {
                throw new ScannerEx("Unterminated string literal", _lineNum);
            }

            var contentEndIdx = _charIdx;
            MoveToNextChar();

            var lexeme = _sourceCode.Substring(startIdx, _charIdx - startIdx);
            var literal = _sourceCode.Substring(contentStartIdx, contentEndIdx - contentStartIdx);

            AddNewToken(TokenType.LiteralStr, lexeme, _lineNum, literal);
        }

        #endregion

        #region Char Pointer Methods

        bool IsCharToScan()
        {
            return _charIdx < _sourceCode.Length;
        }

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

        #region Char Classification Methods

        static bool IsDigitChar(char checkChar)
        {
            return checkChar >= '0' && checkChar <= '9';
        }

        static bool IsAlphaChar(char checkChar)
        {
            return checkChar >= 'a' && checkChar <= 'z' || checkChar >= 'A' && checkChar <= 'Z';
        }

        static bool IsIndentChar(char checkChar)
        {
            return checkChar == ' ' || checkChar == '\t';
        }

        static bool IsFormFeedChar(char checkChar)
        {
            return checkChar == '\f';
        }

        static bool IsNewlineChar(char checkChar)
        {
            return checkChar == '\n' || checkChar == '\r';
        }

        static bool IsCommentPrefix(char checkChar)
        {
            return checkChar == '#';
        }

        static bool IsQuoteChar(char checkChar)
        {
            return checkChar == '\'' || checkChar == '"';
        }

        static bool IsNameLeadingChar(char checkChar)
        {
            return IsAlphaChar(checkChar) || checkChar == '_';
        }

        static bool IsNameTrailChar(char checkChar)
        {
            return IsAlphaChar(checkChar) || IsDigitChar(checkChar) || checkChar == '_';
        }

        #endregion

        #region Helper Methods

        void MovePastNewline()
        {
            switch (CurrChar)
            {
                case '\n':
                {
                    // Unix/Linux/macOS newline
                    MoveToNextChar();
                    break;
                }

                case '\r':
                {
                    // Older Mac newline (if not followed by \n)
                    MoveToNextChar();

                    if (IsCharToScan() && CurrChar == '\n')
                    {
                        // Windows/MS-DOS newline
                        MoveToNextChar();
                    }

                    break;
                }
            }

            _isLineBegin = true;
            _lineNum++;
        }

        void SkipToFirstLexeme()
        {
            while (IsCharToScan())
            {
                if (IsIndentChar(CurrChar))
                {
                    MoveToNextChar();
                }
                else if (IsNewlineChar(CurrChar))
                {
                    MovePastNewline();
                }
                else if (IsCommentPrefix(CurrChar))
                {
                    SkipRemainingLineChars();
                }
                else if (_isLineBegin)
                {
                    return;
                }
                else
                {
                    throw new ScannerEx($"Unexpected indentation.", _lineNum);
                }
            }
        }

        void SkipRemainingLineChars()
        {
            while (IsCharToScan() && !IsNewlineChar(CurrChar))
            {
                MoveToNextChar();
            }
        }

        void AddEndOfCodeToken()
        {
            AddNewToken(TokenType.EndOfCode, string.Empty, _lineNum);
        }

        bool IsLineAndIndentLogicEnabled()
        {
            return _openingBrackets.Count == 0;
        }
        void AddNewToken(TokenType type, string lexeme, int lineNum, object literal = null)
        {
            _tokenList.Add(new Token(type, lexeme, lineNum, literal));
        }

        #endregion
    }
}
