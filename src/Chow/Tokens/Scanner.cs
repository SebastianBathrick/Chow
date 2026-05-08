using System;
using System.Collections.Generic;
using System.Globalization;

namespace Chow.Interpreter.Tokens
{
    sealed class Scanner
    {
        #region Fields & Consts

        const int TAB_SIZE = 4;

        private static readonly IReadOnlyDictionary<string, TokenType> _keywords = new Dictionary<string, TokenType>
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

        readonly List<Token> _tokens;
        readonly Stack<int> _indentLevels;
        readonly Stack<char> _openBracketStack;

        readonly string _srcCode;
        int _scanCharIndex;
        int _currLineNumber;

        bool _isAtStartOfLine;
        bool _isDirty;

        private char CurrentChar => _srcCode[_scanCharIndex];

        #endregion

        #region Constructor & Primary Methods

        public Scanner(string srcCode)
        {
            _srcCode = srcCode;
            _tokens = new List<Token>();
            _scanCharIndex = 0;
            _currLineNumber = 1;
            _indentLevels = new Stack<int>();
            _openBracketStack = new Stack<char>();
            _isAtStartOfLine = true;
            _indentLevels.Push(0);
        }

        public List<Token> ScanTokens()
        {
            ValidateIsNotDirty();

            // If source code is null, emit end of code token, so it can be treated as if it were an empty string or whitespace
            if (_srcCode == null)
            {
                AddEndOfCodeToken();
                return _tokens;
            }

            // Skip to the first line that does not start with whitespace, a comment, or newline character
            SkipToCodeStart();

            while (IsCharToScan())
            {
                RunScanIteration();
            }

            if (_openBracketStack.Count > 0)
            {
                throw new ScannerException("Bracket(s) never closed in source code", _currLineNumber);
            }

            // Add dedent tokens for each block nested within the top-level to mark their end
            AddRemainingDedentTokens();
            AddEndOfCodeToken();

            return _tokens;
        }

        private void ValidateIsNotDirty()
        {
            if (_isDirty)
            {
                throw new InvalidOperationException("This Scanner instance can only be used once.");
            }

            _isDirty = true;
        }

        void RunScanIteration()
        {
            if (_isAtStartOfLine)
            {
                if (IsLineAndIndentLogicEnabled())
                {
                    ScanLineStartIndentation();
                }

                if (!IsCharToScan())
                {
                    return;
                }
            }

            if (IsNameLeadingChar(CurrentChar))
            {
                ScanNameToken();
            }
            else if (IsNewlineChar(CurrentChar))
            {
                ScanNewlineToken();
            }
            else if (IsDigitChar(CurrentChar))
            {
                ScanNumericToken();
            }
            else if (IsIndentChar(CurrentChar))
            {
                MoveToNextChar();
            }
            else if (IsCommentPrefix(CurrentChar))
            {
                SkipRemainingLineChars();
            }
            else if (!TryScanSymbolToken())
            {
                throw new ScannerException($"Unexpected character '{CurrentChar}'.", _currLineNumber);
            }
        }


        #endregion

        #region Newline & Indentation Token Scan Methods

        void ScanNameToken()
        {
            int startIndex = _scanCharIndex;

            while (IsCharToScan() && IsNameTrailChar(CurrentChar))
            {
                MoveToNextChar();
            }

            string lexeme = _srcCode.Substring(startIndex, _scanCharIndex - startIndex);
            TokenType tokenType;

            if (_keywords.TryGetValue(lexeme, out tokenType))
            {
                AddNewToken(tokenType, lexeme, _currLineNumber);
                return;
            }

            AddNewToken(TokenType.Identifier, lexeme, _currLineNumber);
        }

        void ScanNewlineToken()
        {
            // Use a newline for the lexeme for clean debug information
            AddNewToken(TokenType.Newline, "\n", _currLineNumber);
            MovePastNewline();
        }

        void ScanLineStartIndentation()
        {
            int indentColumn = ScanIndentColumn();

            if (!IsCharToScan() || IsNewlineChar(CurrentChar) || IsCommentPrefix(CurrentChar))
            {
                return;
            }

            EmitIndentationTokens(indentColumn);
            _isAtStartOfLine = false;
        }

        int ScanIndentColumn()
        {
            int indentColumn = 0;

            while (IsCharToScan() && IsFormFeedChar(CurrentChar))
            {
                MoveToNextChar();
            }

            while (IsCharToScan() && IsIndentChar(CurrentChar))
            {
                if (CurrentChar == '\t')
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

        void EmitIndentationTokens(int indentColumn)
        {
            int previousIndentColumn = _indentLevels.Peek();

            if (indentColumn > previousIndentColumn)
            {
                _indentLevels.Push(indentColumn);
                AddNewToken(TokenType.Indent, " ", _currLineNumber);
                return;
            }

            if (indentColumn == previousIndentColumn)
            {
                return;
            }

            while (_indentLevels.Peek() > indentColumn)
            {
                _indentLevels.Pop();
                AddNewToken(TokenType.Dedent, string.Empty, _currLineNumber);
            }

            if (_indentLevels.Peek() == indentColumn)
            {
                return;
            }

            throw new ScannerException("Inconsistent dedent.", _currLineNumber);
        }

        void AddRemainingDedentTokens()
        {
            while (_indentLevels.Count > 1)
            {
                _indentLevels.Pop();
                AddNewToken(TokenType.Dedent, string.Empty, _currLineNumber);
            }
        }

        #endregion

        #region Lexeme-Dependent Token Scan Methods

        bool TryScanSymbolToken()
        {
            TokenType tokenType;

            switch (CurrentChar)
            {
                case '(':
                    tokenType = TokenType.SymbolLeftParen;
                    break;

                case ')':
                    tokenType = TokenType.SymbolRightParen;
                    break;

                case ',':
                    tokenType = TokenType.SymbolComma;
                    break;

                case '.':
                    tokenType = TokenType.SymbolDot;
                    break;

                case ':':
                    tokenType = TokenType.SymbolBlockColon;
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
                    _openBracketStack.Push('[');
                    break;

                case ']':
                    tokenType = TokenType.SymbolRightBracket;

                    if (_openBracketStack.Count == 0 || _openBracketStack.Pop() != '[')
                    {
                        throw new ScannerException("Unexpected ']'", _currLineNumber);
                    }
                    break;

                case '{':
                    tokenType = TokenType.SymbolLeftCurly;
                    _openBracketStack.Push('{');
                    break;

                case '}':
                    tokenType = TokenType.SymbolRightCurly;

                    if (_openBracketStack.Count == 0 || _openBracketStack.Pop() != '{')
                    {
                        throw new ScannerException("Unexpected '}'", _currLineNumber);
                    }
                    break;

                default:
                    return false;
            }

            string lexeme = CurrentChar.ToString();
            MoveToNextChar();
            AddNewToken(tokenType, lexeme, _currLineNumber);
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
            AddNewToken(compoundType, compoundStrRep, _currLineNumber);
            return true;
        }

        void ScanNumericToken()
        {
            int startIndex = _scanCharIndex;

            // Move past digits before any decimal point (if any)
            MoveToNextChar();

            while (IsCharToScan() && IsDigitChar(CurrentChar))
            {
                MoveToNextChar();
            }

            // If there is a decimal point, move past it and any following digits
            bool isFloat = IsCharToScan() && CurrentChar == '.';

            if (isFloat)
            {
                MoveToNextChar();

                while (IsCharToScan() && IsDigitChar(CurrentChar))
                {
                    MoveToNextChar();
                }
            }

            int length = _scanCharIndex - startIndex;
            string lexeme = _srcCode.Substring(startIndex, length);
            TokenType numTokenType = isFloat ? TokenType.LiteralFloat : TokenType.LiteralInt;
            object literal;

            try
            {
                if (isFloat)
                {
                    literal = (object)float.Parse(lexeme, CultureInfo.InvariantCulture);
                }
                else
                {
                    literal = int.Parse(lexeme, CultureInfo.InvariantCulture);
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

            AddNewToken(numTokenType, lexeme, _currLineNumber, literal);
        }

        #endregion

        #region Char Pointer Methods

        bool IsCharToScan()
        {
            return _scanCharIndex < _srcCode.Length;
        }

        void MoveToNextChar()
        {
            _scanCharIndex++;
            _isAtStartOfLine = false;
        }

        char PeekNextChar()
        {
            int nextIndex = _scanCharIndex + 1;
            return nextIndex < _srcCode.Length ? _srcCode[nextIndex] : '\0';
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

        private void MovePastNewline()
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

                    if (IsCharToScan() && CurrentChar == '\n')
                    {
                        // Windows/MS-DOS newline
                        MoveToNextChar();
                    }

                    break;
            }

            _currLineNumber++;
            _isAtStartOfLine = true;
        }

        private void SkipToCodeStart()
        {
            while (IsCharToScan())
            {
                if (IsIndentChar(CurrentChar))
                {
                    MoveToNextChar();
                }
                else if (IsNewlineChar(CurrentChar))
                {
                    MovePastNewline();
                }
                else if (IsCommentPrefix(CurrentChar))
                {
                    SkipRemainingLineChars();
                }
                else if (_isAtStartOfLine)
                {
                    return;
                }
                else
                {
                    throw new ScannerException($"Unexpected indentation.", _currLineNumber);
                }
            }
        }

        private void SkipRemainingLineChars()
        {
            while (IsCharToScan() && !IsNewlineChar(CurrentChar))
            {
                MoveToNextChar();
            }
        }

        private void AddEndOfCodeToken()
        {
            AddNewToken(TokenType.EndOfCode, string.Empty, _currLineNumber);
        }

        bool IsLineAndIndentLogicEnabled()
        {
            return _openBracketStack.Count == 0;
        }

        void AddNewToken(TokenType type, string lexeme, int lineNum, object literal = null)
        {
            _tokens.Add(new Token(type, lexeme, lineNum, literal));
        }

        #endregion
    }
}
