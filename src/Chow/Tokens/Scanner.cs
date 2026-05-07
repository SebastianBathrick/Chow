using System;
using System.Collections.Generic;
using System.Globalization;

namespace Chow.Interpreter.Tokens
{
    sealed class Scanner
    {
        #region Fields & Consts

        const int TAB_SIZE = 8;

        private static readonly IReadOnlyDictionary<string, TokenType> _keywords = new Dictionary<string, TokenType>
        {
            { "True", TokenType.True },
            { "False", TokenType.False },
            { "None", TokenType.None },
            { "and", TokenType.And },
            { "or", TokenType.Or },
            { "not", TokenType.Not },
            { "is", TokenType.Is },
            { "in", TokenType.In },
            { "def", TokenType.Def },
            { "return", TokenType.Return },
            { "class", TokenType.Class },
            { "with", TokenType.With },
            { "as", TokenType.As },
            { "global", TokenType.Global },
            { "if", TokenType.If },
            { "else", TokenType.Else },
            { "elif", TokenType.Elif },
            { "for", TokenType.For },
            { "while", TokenType.While },
            { "break", TokenType.Break },
            { "continue", TokenType.Continue },
            { "pass", TokenType.Pass },
            { "try", TokenType.Try },
            { "except", TokenType.Except },
            { "finally", TokenType.Finally },
            { "raise", TokenType.Raise },
            { "assert", TokenType.Assert },
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

            // Skip to the first line that starts with a character that is not whitespace, a comment, or newline character
            SkipToCodeStart();

            while (IsCharToScan())
            {
                RunScanIteration();
            }

            if (_openBracketStack.Count > 0)
            {
                throw new ScannerException("Bracket(s) never closed in source code", _currLineNumber);
            }

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
                    tokenType = TokenType.LeftParenthesis;
                    break;
                
                case ')':
                    tokenType = TokenType.RightParenthesis;
                    break;
                
                case ',':
                    tokenType = TokenType.Comma;
                    break;
                
                case '.':
                    tokenType = TokenType.Dot;
                    break;
                
                case ':':
                    tokenType = TokenType.Colon;
                    break;
                
                case '+':
                    tokenType = TokenType.Plus;
                    break;
                
                case '-':
                    tokenType = TokenType.Minus;
                    break;
                
                case '*':
                    if (TryScanCompoundOp('*', TokenType.StarStar, "**"))
                    {
                        return true;
                    }

                    tokenType = TokenType.Star;
                    break;

                case '/':
                    if (TryScanCompoundOp('/', TokenType.SlashSlash, "//"))
                    {
                        return true;
                    }

                    tokenType = TokenType.Slash;
                    break;

                case '%':
                    tokenType = TokenType.Percent;
                    break;

                case '!':
                    return TryScanCompoundOp('=', TokenType.BangEqual, "!=");

                case '=':
                    if (TryScanCompoundOp('=', TokenType.EqualEqual, "=="))
                    {
                        return true;
                    }

                    tokenType = TokenType.Equal;
                    break;

                case '>':
                    if (TryScanCompoundOp('=', TokenType.GreaterEqual, ">="))
                    {
                        return true;
                    }

                    tokenType = TokenType.Greater;
                    break;

                case '<':
                    if (TryScanCompoundOp('=', TokenType.LessEqual, "<="))
                    {
                        return true;
                    }

                    tokenType = TokenType.Less;
                    break;
                
                // Indentation and line-break rules are not enforced (for lists and dictionary declarations)
                case '[':
                    tokenType = TokenType.LeftBracket;
                    _openBracketStack.Push('[');
                    break;
                
                case ']':
                    tokenType = TokenType.RightBracket;

                    if (_openBracketStack.Count == 0 || _openBracketStack.Pop() != '[')
                    {
                        throw new ScannerException("Unexpected ']'", _currLineNumber);
                    }
                    break;
                
                case '{':
                    tokenType = TokenType.LeftCurlyBrace;
                    _openBracketStack.Push('{');
                    break;
                
                case '}':
                    tokenType = TokenType.RightCurlyBrace;

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
            TokenType numTokenType = isFloat ? TokenType.Float : TokenType.Integer;
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
