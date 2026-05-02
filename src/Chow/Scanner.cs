using System;
using System.Collections.Generic;
using System.Globalization;

namespace Chow
{
    sealed class Scanner
    {
        const int TAB_SIZE = 8;

        List<Token> _tokens = new List<Token>();

        string _srcCode;
        int _scanCharIndex = 0;
        int _currLineNumber = 1;
        Stack<int> _indentLevels = new Stack<int>();
        bool _isAtStartOfLine = true;

        private char CurrentChar => _srcCode[_scanCharIndex];

        public Scanner(string srcCode)
        {
            if (srcCode == null)
            {
                throw new ArgumentNullException(nameof(srcCode));
            }
            _srcCode = srcCode;
        }

        // ============================================================================================================

        public List<Token> ScanTokens()
        {
            _tokens = new List<Token>();
            _scanCharIndex = 0;
            _currLineNumber = 1;
            _indentLevels = new Stack<int>();
            _indentLevels.Push(0);
            _isAtStartOfLine = true;

            bool isWhitespaceOnly = _srcCode.Length > 0;
            for (int i = 0; i < _srcCode.Length && isWhitespaceOnly; i++)
            {
                if (!IsIndentChar(_srcCode[i]) && !IsFormFeedChar(_srcCode[i]))
                {
                    isWhitespaceOnly = false;
                }
            }

            if (isWhitespaceOnly)
            {
                AddNewToken(TokenType.EmptySourceCode, string.Empty, 1);
                return _tokens;
            }

            while (IsCharToScan())
            {
                RunScanIteration();
            }

            AddPendingDedentTokens();
            AddNewToken(TokenType.EndOfCode, string.Empty, _currLineNumber);
            return _tokens;
        }

        void RunScanIteration()
        {
            if (_isAtStartOfLine)
            {
                ScanLineStartIndentation();

                if (!IsCharToScan())
                {
                    return;
                }
            }

            if (IsNewlineChar(CurrentChar))
            {
                ScanNewlineToken();
                return;
            }

            if (IsDigitChar(CurrentChar))
            {
                ScanNumericToken();
                return;
            }

            if (IsIndentChar(CurrentChar))
            {
                throw new ScannerException("Unexpected whitespace.", _currLineNumber);
            }

            throw new ScannerException($"Unexpected character '{CurrentChar}'.", _currLineNumber);
        }

        void AddNewToken(TokenType type, string lexeme, int lineNum, object literal = null)
        {
            _tokens.Add(new Token(type, lexeme, lineNum, literal));
        }

        // ============================================================================================================

        void ScanNewlineToken()
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

            // Use a newline for the lexeme for clean debug information
            AddNewToken(TokenType.Newline, "\n", _currLineNumber);
            _currLineNumber++;
            _isAtStartOfLine = true;
        }

        void ScanLineStartIndentation()
        {
            int indentColumn = ScanIndentColumn();

            if (!IsCharToScan() || IsNewlineChar(CurrentChar))
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

            if (_indentLevels.Peek() != indentColumn)
            {
                throw new ScannerException("Inconsistent dedent.", _currLineNumber);
            }
        }

        void AddPendingDedentTokens()
        {
            while (_indentLevels.Count > 1)
            {
                _indentLevels.Pop();
                AddNewToken(TokenType.Dedent, string.Empty, _currLineNumber);
            }
        }

        // ============================================================================================================

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
                literal = isFloat
                    ? (object)float.Parse(lexeme, CultureInfo.InvariantCulture)
                    : (object)int.Parse(lexeme, CultureInfo.InvariantCulture);
            }
            catch (OverflowException)
            {
                throw new OverflowException(
                    $"{numTokenType} literal value out of range & parsing failed. Literal Value: {lexeme}");
            }
            catch (FormatException)
            {
                // This should never occur, and if it does, the scanner logic is incorrect
                throw new InvalidOperationException();
            }

            AddNewToken(numTokenType, lexeme, _currLineNumber, literal);
        }

        #region Char Pointer Methods

        bool IsCharToScan()
        {
            return _scanCharIndex < _srcCode.Length;
        }

        void MoveToNextChar()
        {
            _scanCharIndex++;
        }

        #endregion

        #region Char Classification Methods

        static bool IsDigitChar(char checkChar)
        {
            return checkChar >= '0' && checkChar <= '9';
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

        #endregion
    }
}
