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
        int _currIndentLvl = 0;

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
            _currIndentLvl = 0;

            bool isWhitespaceOnly = _srcCode.Length > 0;
            for (int i = 0; i < _srcCode.Length && isWhitespaceOnly; i++)
            {
                if (!IsIndentChar(_srcCode[i]))
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

            AddNewToken(TokenType.EndOfCode, string.Empty, _currLineNumber);
            return _tokens;
        }

        void RunScanIteration()
        {
            if (IsNewlineChar(CurrentChar))
            {
                TryCreateNewlineToken();
                TryScanIndentTokens();
                return;
            }

            if (IsDigitChar(CurrentChar))
            {
                ScanNumericToken();
                return;
            }

            if (IsIndentChar(CurrentChar))
            {
                throw new InvalidOperationException(
                    $"Unexpected indentation at line {_currLineNumber}.");
            }
        }

        void AddNewToken(TokenType type, string lexeme, int lineNum, object literal = null)
        {
            _tokens.Add(new Token(type, lexeme, lineNum, literal));
        }

        // ============================================================================================================

        bool TryCreateNewlineToken()
        {
            bool isNewline = false;

            switch (CurrentChar)
            {
                case '\n':
                    // Unix/Linux/macOS newline
                    isNewline = true;
                    MoveToNextChar();
                    break;

                case '\r':
                    // Older Mac newline (if not followed by \n)
                    isNewline = true;
                    MoveToNextChar();

                    if (IsCharToScan() && CurrentChar == '\n')
                    {
                        // Windows/MS-DOS newline
                        MoveToNextChar();
                    }
                    break;        
            }

            if (isNewline)
            {
                // Use a newline for the lexeme for clean debug information
                AddNewToken(TokenType.Newline, "\n", _currLineNumber);
                _currLineNumber++;
            }

            // True will indicate that the current char is at the start of a new line (if there is a char)
            return isNewline;
        }


        bool TryScanIndentTokens()
        {
            int spaceCount = 0;

            while (IsCharToScan() && IsIndentChar(CurrentChar))
            {
                if (CurrentChar == '\t')
                {
                    // Tab rounds column up to the next multiple of TAB_SIZE (Python rule)
                    spaceCount = ((spaceCount / TAB_SIZE) + 1) * TAB_SIZE;
                }
                else
                {
                    spaceCount++;
                }

                MoveToNextChar();
            }

            // If its the end of the src code there is no need to change scopes, so skip the new indent/dedents
            // If there is a newline char then this line is empty, and the line should be effectively ignored
            if (!IsCharToScan() || IsNewlineChar(CurrentChar))
            {
                return false;
            }

            if (spaceCount > _currIndentLvl)
            {
                AddNewToken(TokenType.Indent, " ", _currLineNumber);
            }
            else if (spaceCount < _currIndentLvl)
            {
                AddNewToken(TokenType.Dedent, string.Empty, _currLineNumber);
            }

            _currIndentLvl = spaceCount;
            return true;
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

        static bool IsNewlineChar(char checkChar)
        {
            return checkChar == '\n' || checkChar == '\r';
        }

        #endregion
    }
}