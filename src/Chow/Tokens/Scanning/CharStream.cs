namespace Chow.Tokens.Scanning
{
    /// <summary>Represents a sequence of characters that are iteratively evaluated.</summary> 
    /// <remarks>Instances skip blank lines and track the selected char's current line number.</remarks>
    public class CharStream
    {
        readonly string _text;
        int _selectedIndex;
        int _line;
        bool _isFirstInLine; 
        
        /// <summary>Whether the selected char is the first of its line.</summary>
        public bool IsFirstInLine => _isFirstInLine;
        
        /// <summary>Whether the selected char is a '\n' or '\r'.</summary>
        public bool HasLineEnded => Is('\n') || Is('\r');
        
        /// <summary>Whether there is no selected char because the stream ended.</summary>
        public bool IsEndOfStream => _selectedIndex == _text.Length;

        /// <summary>The selected char's line number.</summary>
        /// <remarks>If <see cref="IsEndOfStream"/> is true then -1 will be returned.</remarks>
        public int LineNumber => !IsEndOfStream ? _line : END_OF_STREAM_LINE;
        
        char SelectedChar => !IsEndOfStream ? _text[_selectedIndex] : END_MARKER_CHAR;
        
        /// <summary>Initializes a new stream populated with chars.</summary>
        /// <param name="text">string containing chars the stream will contain or null.</param>
        public CharStream(string text)
        {
            _text = text ?? string.Empty;
            _selectedIndex = 0;
            _line = _text.Length > 0 ? FIRST_LINE : END_OF_STREAM_LINE;
            _isFirstInLine = true;
        }

        /// <summary>Selects the next char or reaches the end of the stream.</summary>
        public void Next()
        {
            if (IsEndOfStream)
            {
                return;
            }
            
            if (!HasLineEnded)
            {
                _selectedIndex++;
                _isFirstInLine = false;
                return;
            }

            do
            {
                NextLine();
            }
            while (HasLineEnded);
        }

        /// <summary>Selects the next non-whitespace char or reaches end-of the stream.</summary>
        public void NextNonWhitespace()
        {
            while (IsWhitespace())
            {
                Next();
            }
        }
        
        // Precondition: The current char was checked, and it is '\n' or '\r'
        void NextLine()
        {
            // --- Cases ---
            // - \n (Unix/Linux/macOS)
            // - \r (Older Macs)
            // - \r\n (Windows/MS-DOS)

            _selectedIndex += Is('\r') && IsNext('\n') 
                ? WINDOWS_NEWLINE_LEN : UNIX_MAC_NEWLINE_LEN;

            _line++;
            _isFirstInLine = !IsEndOfStream;
        }

        /// <summary>Whether the selected char is the value provided.</summary>
        /// <param name="checkChar">char to compare the selected char to.</param>
        public bool Is(char checkChar)
        {
            return SelectedChar == checkChar;
        }

        /// <summary>Whether the next char is the value provided.</summary>
        /// <param name="checkChar">char to compare the next char to.</param>
        public bool IsNext(char checkChar)
        {
            var nextIndex = _selectedIndex + 1;
            return nextIndex != _text.Length &&  _text[nextIndex] == checkChar;
        }
        
        /// <summary>Whether the selected char is a digit.</summary>
        public bool IsDigit()
        {
            return SelectedChar >= '0' && SelectedChar <= '9';
        }
        
        /// <summary>Whether the selected char is a letter.</summary>
        public bool IsLetter()
        {
            return SelectedChar >= 'a' && SelectedChar <= 'z' 
                || SelectedChar >= 'A' && SelectedChar <= 'Z';
        }
        
        /// <summary>Whether the selected char is a double quote.</summary>
        public bool IsDoubleQuote()
        {
            return Is('\'') || Is('"');
        }

        /// <summary>Whether the selected char is a tab or space.</summary>
        public bool IsWhitespace()
        {
            return Is(' ') || Is('\t');
        }
        
        /// <summary>Whether the selected char is a double quote.</summary>
        public bool IsFormFeed()
        {
            return Is('\f');
        }
        
        const char END_MARKER_CHAR = '\0';
        const int WINDOWS_NEWLINE_LEN = 2;
        const int UNIX_MAC_NEWLINE_LEN = 1;
        const int END_OF_STREAM_LINE = -1;
        const int FIRST_LINE = 1;
    }
}
