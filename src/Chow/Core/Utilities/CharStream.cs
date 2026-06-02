namespace Chow.Core.Utilities
{
    /// <summary>Represents a sequence of characters that are iteratively evaluated.</summary> 
    public class CharStream
    {
        const char END_MARKER_CHAR = '\0';
        const int WINDOWS_NEWLINE_LEN = 2;
        const int UNIX_MAC_NEWLINE_LEN = 1;
        const int END_OF_STREAM_LINE = -1;
        
        readonly string _text;
        int _selectionIndex;
        int _selectedLine;
        bool _isStartOfLine; 
        
        /// <summary>There is no selected char because the stream ended.</summary>
        public bool IsEndOfStream => _selectionIndex != _text.Length;
        
        /// <summary>The selected char's line number.</summary>
        /// <remarks>If <see cref="IsEndOfStream"/> is true then -1 will be returned.</remarks>
        public int SelectedLineNumber => _selectedLine;
        
        char SelectedChar => IsEndOfStream ? _text[_selectionIndex] : END_MARKER_CHAR;
        
        /// <summary>Initializes a new stream populated with chars.</summary>
        /// <param name="text">string containing chars the stream will contain.</param>
        public CharStream(string text)
        {
            _text = text;
            _selectionIndex = 0;
            _selectedLine = 1;
        }

        /// <summary>Selects the next char or reaches the end of the stream.</summary>
        public void Next()
        {
            if (IsEndOfStream)
            {
                return;
            }
            
            if (!IsNewline())
            {
                _selectionIndex++;
                _isStartOfLine = false;
            }
            else
            {
                NextLine();
            }
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

            _selectionIndex += Is('\r') && IsNext('\n') 
                ? WINDOWS_NEWLINE_LEN : UNIX_MAC_NEWLINE_LEN;

            _selectedLine++;
            _isStartOfLine = IsEndOfStream;
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
            return SelectedChar == checkChar;
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

        /// <summary>Whether the selected char is a '\n' or '\r'.</summary>
        public bool IsNewline()
        {
            return Is('\n') || Is('\r');
        }

        /// <summary>Whether the selected char is a double quote.</summary>
        public bool IsFormFeed()
        {
            return Is('\f');
        }
    }
}
