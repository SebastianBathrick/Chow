using System;
using System.Collections.Generic;

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
            _srcCode = srcCode;
        }

        public List<Token> ScanTokens()
        {
            while (IsCharToScan())
            {
                if (IsNewlineChar(CurrentChar) && TryIncrementLineNumber())
                {

                }
                

                if (IsDigitChar(CurrentChar))
                {
                    ScanNumericToken();
                    continue;
                }
            }
        }

        bool TryIncrementLineNumber()
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
                _currLineNumber++;

                // Use a newline for the lexeme for clean debug information
                AddNewToken(TokenType.Newline, "\n", _currLineNumber);
            }

            // True will indicate that the current char is at the start of a new line (if there is a char)
            return isNewline;
        }

        void AddNewToken(TokenType type, string lexeme, int lineNum, object literal = null)
        {
            _tokens.Add(new Token(type, lexeme, lineNum, literal));
        }

        void ScanIndentLevelTokens()
        {
            int spaceCount = 0;

            while (IsCharToScan() && IsIndentChar(CurrentChar))
            {
                var isTab = CurrentChar == '\t';
                spaceCount += isTab ? TAB_SIZE : 1;
                MoveToNextChar();
            }

            // If its the end of the src code there is no need to change scopes, so skip the new indent/dedents
            // If there is a newline char then this line is empty, and the line should be effectively ignored
            if (!IsCharToScan() || IsNewlineChar(CurrentChar))
            {
                return;
            }

            int prevIndentLvl = _currIndentLvl;
            int indentLvlChange = spaceCount - prevIndentLvl;
            _currIndentLvl = prevIndentLvl + indentLvlChange;

            while (indentLvlChange != 0)
            {
                TokenType newTokenType = indentLvlChange > 0 ? TokenType.Indent : TokenType.Dedent;
                string lexeme;

                if (newTokenType == TokenType.Indent)
                {
                    indentLvlChange--;
                    lexeme = " ";
                }
                else
                {
                    indentLvlChange++;
                    lexeme = string.Empty;
                }

            }
        }

        void ScanNumericToken()
        {
            int startIndex = _scanCharIndex;
            int length = 1;

            // Move past digits before any decimal point (if any)
            MoveToNextChar();

            while (IsCharToScan() && IsDigitChar(CurrentChar))
            {
                MoveToNextChar();
                length++;
            }

            // If there is a decimal point, move past it and any following digits
            bool isFloat = IsCharToScan() && CurrentChar == '.';

            if (isFloat)
            {
                MoveToNextChar();
                length++;

                while (IsCharToScan() && IsDigitChar(CurrentChar))
                {
                    MoveToNextChar();
                    length++;
                }
            }

            string lexeme = _srcCode.Substring(startIndex, length);
            object literal = null;

            try
            {
                literal = isFloat ? (object)float.Parse(lexeme) : int.Parse(lexeme);
            }
            catch (FormatException)
            {
                // This should never occur, and if it does, the scanner logic is incorrect
                throw new InvalidOperationException();
            }

            AddNewToken(isFloat ? TokenType.Float : TokenType.Integer, lexeme, _currLineNumber, literal);
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