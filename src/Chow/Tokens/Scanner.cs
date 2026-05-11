using System;
using System.Collections.Generic;
using System.Globalization;

namespace Chow.Interpreter.Tokens
{
    sealed class Scanner
    {
        #region Fields & Consts

        const int TAB_SIZE = 4;

        static readonly IReadOnlyDictionary<string, TokenType> _keywords = new Dictionary<string, TokenType>
        {
            { "True", TokenType.KeywordTrue },
            { "False", TokenType.KeywordFalse },
            { "None", TokenType.KeywordNone },
            { "and", TokenType.KeywordAnd },
            { "or", TokenType.KeywordOr },
            { "not", TokenType.KeywordNot },
            { "is", TokenType.KeywordIs },
            { "in", TokenType.KeywordIn },
            { "def", TokenType.KeywordDef },
            { "return", TokenType.KeywordReturn },
            { "class", TokenType.KeywordClass },
            { "with", TokenType.KeywordWith },
            { "as", TokenType.KeywordAs },
            { "global", TokenType.KeywordGlobal },
            { "if", TokenType.KeywordIf },
            { "else", TokenType.KeywordElse },
            { "elif", TokenType.KeywordElif },
            { "for", TokenType.KeywordFor },
            { "while", TokenType.KeywordWhile },
            { "break", TokenType.KeywordBreak },
            { "continue", TokenType.KeywordContinue },
            { "pass", TokenType.KeywordPass },
            { "try", TokenType.KeywordTry },
            { "except", TokenType.KeywordExcept },
            { "finally", TokenType.KeywordFinally },
            { "raise", TokenType.KeywordRaise },
            { "assert", TokenType.KeywordAssert },
        };

        readonly List<Token> _tkns;
        readonly Stack<int> _indentLvls;
        readonly Stack<char> _brackets;

        readonly string _src;
        int _charIdx;
        int _lineNum;

        bool _isLineBegin;

        char CurrChar => _src[_charIdx];

        #endregion

        #region Constructor & Primary Methods

        public Scanner(string src)
        {
            _src = src;
            _tkns = new List<Token>();
            _charIdx = 0;
            _lineNum = 1;
            _indentLvls = new Stack<int>();
            _brackets = new Stack<char>();
            _isLineBegin = true;
            _indentLvls.Push(0);
        }

        public List<Token> ScanTokens()
        {
            // If source code is null, emit end of code token, so it can be treated as if it were an empty string or whitespace
            if (_src == null)
            {
                AddEndOfCodeTkn();
                return _tkns;
            }

            // Skip to the first line that does not start with whitespace, a comment, or newline character
            SkipToFirstLexeme();

            while (IsCharToScan())
            {
                RunScanIteration();
            }

            if (_brackets.Count > 0)
            {
                throw new ScannerEx("Bracket(s) never closed in source code", _lineNum);
            }

            // Add dedent tokens for each block nested within the top-level to mark their end
            AddLastDedentsTkns();
            AddEndOfCodeTkn();

            return _tkns;
        }

        void RunScanIteration()
        {
            if (_isLineBegin)
            {
                if (IsLineAndIndentLogicEnabled())
                {
                    ScanIndentTkn();
                }

                if (!IsCharToScan())
                {
                    return;
                }
            }

            if (IsNameLeadingChar(CurrChar))
            {
                ScanNameTkn();
            }
            else if (IsNewlineChar(CurrChar))
            {
                ScanNewlineTkn();
            }
            else if (IsDigitChar(CurrChar))
            {
                ScanNumericToken();
            }
            else if (IsQuoteChar(CurrChar))
            {
                ScanStringTkn();
            }
            else if (IsIndentChar(CurrChar))
            {
                MoveToNextChar();
            }
            else if (IsCommentPrefix(CurrChar))
            {
                SkipRemainingLineChars();
            }
            else if (!TryScanSymbolTkn())
            {
                throw new ScannerEx($"Unexpected character '{CurrChar}'.", _lineNum);
            }
        }


        #endregion

        #region Newline & Indentation Token Scan Methods

        void ScanNameTkn()
        {
            int startIdx = _charIdx;

            while (IsCharToScan() && IsNameTrailChar(CurrChar))
            {
                MoveToNextChar();
            }

            string lexeme = _src.Substring(startIdx, _charIdx - startIdx);
            TokenType tknType;

            if (_keywords.TryGetValue(lexeme, out tknType))
            {
                AddNewToken(tknType, lexeme, _lineNum);
                return;
            }

            AddNewToken(TokenType.Identifier, lexeme, _lineNum);
        }

        void ScanNewlineTkn()
        {
            // Newlines are ignored when inside brackets
            if (_brackets.Count == 0)
            {
                // Use a newline for the lexeme for clean debug information
                AddNewToken(TokenType.Newline, "\n", _lineNum);
            }

            MovePastNewline();
        }

        void ScanIndentTkn()
        {
            int indentColumn = ScanIndentColumn();

            if (!IsCharToScan() || IsNewlineChar(CurrChar) || IsCommentPrefix(CurrChar))
            {
                return;
            }

            CreateIndentTkns(indentColumn);
            _isLineBegin = false;
        }

        int ScanIndentColumn()
        {
            int indentColumn = 0;

            while (IsCharToScan() && IsFormFeedChar(CurrChar))
            {
                MoveToNextChar();
            }

            while (IsCharToScan() && IsIndentChar(CurrChar))
            {
                if (CurrChar == '\t')
                {
                    // Tab rounds column up to the next multiple of TAB_SIZE (Python rule)
                    indentColumn = ((indentColumn / TAB_SIZE) + 1) * TAB_SIZE;
                }
                else
                {
                    indentColumn++;
                }

                MoveToNextChar();
            }

            return indentColumn;
        }

        void CreateIndentTkns(int indentLvl)
        {
            int prevIndentLvl = _indentLvls.Peek();

            if (indentLvl > prevIndentLvl)
            {
                _indentLvls.Push(indentLvl);
                AddNewToken(TokenType.Indent, " ", _lineNum);
                return;
            }

            if (indentLvl == prevIndentLvl)
            {
                return;
            }

            while (_indentLvls.Peek() > indentLvl)
            {
                _indentLvls.Pop();
                AddNewToken(TokenType.Dedent, string.Empty, _lineNum);
            }

            if (_indentLvls.Peek() != indentLvl)
            {
                throw new ScannerEx("Inconsistent dedent.", _lineNum);
            }
        }

        void AddLastDedentsTkns()
        {
            while (_indentLvls.Count > 1)
            {
                _indentLvls.Pop();
                AddNewToken(TokenType.Dedent, string.Empty, _lineNum);
            }
        }

        #endregion

        #region Lexeme-Dependent Token Scan Methods

        // TODO: Refactor all project switches to use curly braces
        bool TryScanSymbolTkn()
        {
            TokenType tknType;

            switch (CurrChar)
            {
                case ',':
                    {
                        tknType = TokenType.SymbolComma;
                        break;
                    }


                case '.':
                    {
                        tknType = TokenType.SymbolDot;
                        break;
                    }

                case ':':
                    {
                        tknType = TokenType.SymbolColon;
                        break;
                    }

                case '+':
                    {
                        tknType = TokenType.SymbolPlus;
                        break;
                    }

                case '-':
                    {
                        tknType = TokenType.SymbolMinus;
                        break;
                    }

                case '*':
                    {
                        if (TryScanCompoundOp('*', TokenType.SymbolExponent, "**"))
                        {
                            return true;
                        }

                        tknType = TokenType.SymbolMultiply;
                        break;
                    }

                case '/':
                    {
                        if (TryScanCompoundOp('/', TokenType.SymbolFloorDivide, "//"))
                        {
                            return true;
                        }

                        tknType = TokenType.SymbolDivide;
                        break;
                    }

                case '%':
                    {
                        tknType = TokenType.SymbolPercent;
                        break;
                    }

                case '|':
                    {
                        tknType = TokenType.SymbolPipe;
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

                        tknType = TokenType.SymbolAssign;
                        break;
                    }

                case '>':
                    {
                        if (TryScanCompoundOp('=', TokenType.SymbolGreaterEqual, ">="))
                        {
                            return true;
                        }

                        tknType = TokenType.SymbolGreater;
                        break;
                    }
                case '<':
                    {
                        if (TryScanCompoundOp('=', TokenType.SymbolLessEqual, "<="))
                        {
                            return true;
                        }

                        tknType = TokenType.SymbolLess;
                        break;
                    }

                // Indentation and line-break rules are not enforced (for lists and dictionary declarations)
                case '[':
                    {
                        tknType = TokenType.SymbolLeftBracket;
                        _brackets.Push('[');
                        break;
                    }

                // TODO: Refactor to reduce repeated closing bracket logic
                case ']':
                    {
                        tknType = TokenType.SymbolRightBracket;

                        if (_brackets.Count == 0 || _brackets.Pop() != '[')
                        {
                            throw new ScannerEx("Unexpected ']'", _lineNum);
                        }
                        break;
                    }

                case '{':
                    {
                        tknType = TokenType.SymbolLeftCurly;
                        _brackets.Push('{');
                        break;
                    }

                case '}':
                    {
                        tknType = TokenType.SymbolRightCurly;

                        if (_brackets.Count == 0 || _brackets.Pop() != '{')
                        {
                            throw new ScannerEx("Unexpected '}'", _lineNum);
                        }
                        break;
                    }

                case '(':
                    {
                        tknType = TokenType.SymbolLeftParen;
                        _brackets.Push('(');
                        break;
                    }

                case ')':
                    {
                        tknType = TokenType.SymbolRightParen;

                        if (_brackets.Count == 0 || _brackets.Pop() != '(')
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

            string lexeme = CurrChar.ToString();
            MoveToNextChar();
            AddNewToken(tknType, lexeme, _lineNum);
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
            int startIdx = _charIdx;

            // Move past digits before any decimal point (if any)
            MoveToNextChar();

            while (IsCharToScan() && IsDigitChar(CurrChar))
            {
                MoveToNextChar();
            }

            // If there is a decimal point, move past it and any following digits
            bool isFloat = IsCharToScan() && CurrChar == '.';

            if (isFloat)
            {
                MoveToNextChar();

                while (IsCharToScan() && IsDigitChar(CurrChar))
                {
                    MoveToNextChar();
                }
            }

            int len = _charIdx - startIdx;
            string lexeme = _src.Substring(startIdx, len);
            TokenType numTknType = isFloat ? TokenType.LiteralFloat : TokenType.LiteralInt;
            object literal;

            try
            {
                if (isFloat)
                {
                    literal = float.Parse(lexeme, CultureInfo.InvariantCulture);
                }
                else
                {
                    literal = int.Parse(lexeme, CultureInfo.InvariantCulture);
                }
            }
            catch (OverflowException)
            {
                throw new OverflowException($"{numTknType} literal value out of range & parsing failed. Literal Value: {lexeme}");
            }
            catch (FormatException)
            {
                // This should never occur, and if it does, the scanner logic is incorrect
                throw new InvalidOperationException();
            }

            AddNewToken(numTknType, lexeme, _lineNum, literal);
        }

        void ScanStringTkn()
        {
            char quoteChar = CurrChar;
            int startIdx = _charIdx;
            MoveToNextChar();

            int contentStartIdx = _charIdx;

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

            int contentEndIdx = _charIdx;
            MoveToNextChar();

            string lexeme = _src.Substring(startIdx, _charIdx - startIdx);
            string literal = _src.Substring(contentStartIdx, contentEndIdx - contentStartIdx);

            AddNewToken(TokenType.LiteralStr, lexeme, _lineNum, literal);
        }

        #endregion

        #region Char Pointer Methods

        bool IsCharToScan()
        {
            return _charIdx < _src.Length;
        }

        void MoveToNextChar()
        {
            _charIdx++;
            _isLineBegin = false;
        }

        char PeekNextChar()
        {
            int nextIndex = _charIdx + 1;
            return nextIndex < _src.Length ? _src[nextIndex] : '\0';
        }

        #endregion

        #region Char Classification Methods

        static bool IsDigitChar(char checkChar)
        {
            return checkChar >= '0' && checkChar <= '9';
        }

        static bool IsAlphaChar(char checkChar)
        {
            return (checkChar >= 'a' && checkChar <= 'z') || (checkChar >= 'A' && checkChar <= 'Z');
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
                    // Unix/Linux/macOS newline
                    MoveToNextChar();
                    break;

                case '\r':
                    // Older Mac newline (if not followed by \n)
                    MoveToNextChar();

                    if (IsCharToScan() && CurrChar == '\n')
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

        void AddEndOfCodeTkn()
        {
            AddNewToken(TokenType.EndOfCode, string.Empty, _lineNum);
        }

        bool IsLineAndIndentLogicEnabled()
        {
            return _brackets.Count == 0;
        }
        void AddNewToken(TokenType type, string lexeme, int lineNum, object literal = null)
        {
            _tkns.Add(new Token(type, lexeme, lineNum, literal));
        }

        #endregion
    }
}
