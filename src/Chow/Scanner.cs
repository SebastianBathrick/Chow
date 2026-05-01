using System;
using System.Collections.Generic;
using System.Globalization;

namespace Chow
{
    /// <summary>
    /// Converts Chow source code into a flat sequence of tokens.
    /// </summary>
    internal sealed class Scanner
    {
        private static readonly Dictionary<string, TokenType> Keywords = new Dictionary<string, TokenType>
        {
            { "and", TokenType.And },
            { "class", TokenType.Class },
            { "def", TokenType.Def },
            { "elif", TokenType.Elif },
            { "else", TokenType.Else },
            { "False", TokenType.False },
            { "for", TokenType.For },
            { "if", TokenType.If },
            { "in", TokenType.In },
            { "None", TokenType.None },
            { "not", TokenType.Not },
            { "or", TokenType.Or },
            { "pass", TokenType.Pass },
            { "return", TokenType.Return },
            { "True", TokenType.True },
            { "while", TokenType.While }
        };

        private readonly string _sourceCode;
        private readonly List<Token> _scannedTokens;
        private readonly Stack<int> _currIndentLevels;

        private int _lexemeStartIndex;
        private int _currCharIndex;
        private int _currLineNum;
        private int _groupingDepth;
        private bool _isAtStartOfLine;

        /// <summary>
        /// Creates a scanner for the supplied source code.
        /// </summary>
        /// <param name="sourceCode">The complete source code to scan.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="sourceCode"/> is null.</exception>
        public Scanner(string sourceCode)
        {
            _sourceCode = sourceCode ?? throw new ArgumentNullException(nameof(sourceCode));
            _scannedTokens = new List<Token>();
            _currIndentLevels = new Stack<int>();
            _currIndentLevels.Push(0);
            _currLineNum = 1;
            _isAtStartOfLine = true;
        }

        /// <summary>
        /// Scans the full source code and returns tokens in source order.
        /// </summary>
        /// <returns>A token list ending with <see cref="TokenType.EndOfFile"/>.</returns>
        /// <exception cref="ScannerException">
        /// Thrown when the scanner encounters invalid characters, invalid indentation,
        /// unterminated strings, or unterminated grouped expressions.
        /// </exception>
        public List<Token> ScanTokens()
        {
            while (!IsAtEnd())
            {
                // Indentation is meaningful only at the beginning of a logical line.
                // Newlines inside grouping pairs continue the same logical line.
                if (_isAtStartOfLine && _groupingDepth == 0 && ScanLinePrefix())
                {
                    continue;
                }

                _lexemeStartIndex = _currCharIndex;
                ScanNextToken();
            }

            if (_groupingDepth > 0)
            {
                throw CreateError("Unterminated grouped expression.");
            }

            // Close the token stream in the shape expected by a parser:
            // a final statement newline, all open block dedents, then EOF.
            AddImplicitFinalNewLine();
            AddRemainingDedents();
            AddToken(TokenType.EndOfFile, string.Empty, null);

            return _scannedTokens;
        }

        private bool ScanLinePrefix()
        {
            // The line prefix decides whether a physical line contributes indentation
            // tokens, is skipped as blank/comment-only, or should scan a real token next.
            int indentStartIndex = _currCharIndex;
            int indentWidth = 0;

            while (Peek() == ' ' || Peek() == '\t')
            {
                if (Peek() == '\t')
                {
                    throw CreateError("Tabs are not supported in indentation.");
                }

                Advance();
                indentWidth++;
            }

            if (IsAtEnd())
            {
                return true;
            }

            if (Peek() == '#')
            {
                SkipComment();
                ConsumeLineBreakIfPresent();
                return true;
            }

            if (IsLineBreakStart(Peek()))
            {
                ConsumeLineBreakIfPresent();
                return true;
            }

            int currIndentWidth = _currIndentLevels.Peek();
            
            if (indentWidth <= currIndentWidth)
            {
                // Dedents must land on a previously seen indentation width; otherwise the
                // block structure is ambiguous and should fail during scanning.
                while (indentWidth < _currIndentLevels.Peek())
                {
                    _currIndentLevels.Pop();
                    AddToken(TokenType.Dedent, string.Empty, null);
                }

                if (indentWidth != _currIndentLevels.Peek())
                {
                    throw CreateError("Indentation does not match any previous indentation level.");
                }
            }
            else
            {
                _currIndentLevels.Push(indentWidth);
                AddToken(TokenType.Indent, _sourceCode.Substring(indentStartIndex, indentWidth), null);
            }

            _isAtStartOfLine = false;
            return false;
        }

        private void ScanNextToken()
        {
            char currChar = Advance();

            switch (currChar)
            {
                case '(':
                    // Grouping depth suppresses NewLine, Indent, and Dedent tokens until
                    // the matching close delimiter is scanned.
                    _groupingDepth++;
                    AddToken(TokenType.LeftParenthesis);
                    break;
                
                case ')':
                    DecreaseGroupingDepth();
                    AddToken(TokenType.RightParenthesis);
                    break;
                
                case '[':
                    _groupingDepth++;
                    AddToken(TokenType.LeftBracket);
                    break;
                
                case ']':
                    DecreaseGroupingDepth();
                    AddToken(TokenType.RightBracket);
                    break;
                
                case ',':
                    AddToken(TokenType.Comma);
                    break;
                
                case '.':
                    AddToken(TokenType.Dot);
                    break;
                
                case ':':
                    AddToken(TokenType.Colon);
                    break;
                
                case '+':
                    AddToken(Match('=') ? TokenType.PlusEqual : TokenType.Plus);
                    break;
                
                case '-':
                    AddToken(Match('=') ? TokenType.MinusEqual : TokenType.Minus);
                    break;
                
                case '*':
                    AddToken(Match('=') ? TokenType.StarEqual : TokenType.Star);
                    break;
                
                case '/':
                    AddToken(Match('=') ? TokenType.SlashEqual : TokenType.Slash);
                    break;
                
                case '%':
                    AddToken(Match('=') ? TokenType.PercentEqual : TokenType.Percent);
                    break;
                
                case '=':
                    AddToken(Match('=') ? TokenType.EqualEqual : TokenType.Equal);
                    break;
                
                case '!':
                    if (!Match('='))
                    {
                        throw CreateError("Unexpected character.");
                    }

                    AddToken(TokenType.BangEqual);
                    break;
                
                case '<':
                    AddToken(Match('=') ? TokenType.LessEqual : TokenType.Less);
                    break;
                
                case '>':
                    AddToken(Match('=') ? TokenType.GreaterEqual : TokenType.Greater);
                    break;
                
                case '#':
                    SkipComment();
                    break;
                
                case ' ':
                case '\t':
                    break;
                
                case '\r':
                    if (Match('\n'))
                    {
                        AddLineBreakTokenIfSignificant();
                    }
                    else
                    {
                        AddLineBreakTokenIfSignificant();
                    }
                    break;
                
                case '\n':
                    AddLineBreakTokenIfSignificant();
                    break;
                
                case '\'':
                case '"':
                    ScanStringLiteral(currChar);
                    break;
                
                default:
                    if (IsDigit(currChar))
                    {
                        ScanNumberLiteral();
                        return;
                    }
                    
                    if (!IsIdentifierStart(currChar))
                    {
                        throw CreateError("Unexpected character.");
                    }

                    ScanIdentifierOrKeyword();
                    break;
            }
        }

        private void ScanStringLiteral(char quoteChar)
        {
            while (!IsAtEnd())
            {
                if (Peek() == '\\')
                {
                    // Escaped characters stay in the literal text, but the escaped quote
                    // must not terminate the string token.
                    Advance();

                    if (!IsAtEnd())
                    {
                        Advance();
                    }

                    continue;
                }

                if (Peek() == quoteChar)
                {
                    break;
                }

                if (IsLineBreakStart(Peek()))
                {
                    throw CreateError("Unterminated string.");
                }

                Advance();
            }

            if (IsAtEnd())
            {
                throw CreateError("Unterminated string.");
            }

            Advance();

            string literalVal = _sourceCode.Substring(_lexemeStartIndex + 1, _currCharIndex - _lexemeStartIndex - 2);
            AddToken(TokenType.String, literalVal);
        }

        private void ScanNumberLiteral()
        {
            while (IsDigit(Peek()))
            {
                Advance();
            }

            if (Peek() == '.' && IsDigit(PeekNext()))
            {
                // A dot belongs to a number only when it starts a fractional part.
                // This keeps "123." tokenized as Number + Dot.
                Advance();

                while (IsDigit(Peek()))
                {
                    Advance();
                }
            }

            string numLexeme = _sourceCode.Substring(_lexemeStartIndex, _currCharIndex - _lexemeStartIndex);
            AddToken(TokenType.Number, double.Parse(numLexeme, CultureInfo.InvariantCulture));
        }

        private void ScanIdentifierOrKeyword()
        {
            while (IsIdentifierPart(Peek()))
            {
                Advance();
            }

            string text = _sourceCode.Substring(_lexemeStartIndex, _currCharIndex - _lexemeStartIndex);
            TokenType tokenType;
            
            if (!Keywords.TryGetValue(text, out tokenType))
            {
                tokenType = TokenType.Identifier;
            }

            AddToken(tokenType);
        }

        private void AddLineBreakTokenIfSignificant()
        {
            // Physical newlines inside () or [] are line continuations.
            bool isSignificant = _groupingDepth == 0;

            if (isSignificant)
            {
                AddToken(TokenType.NewLine);
            }

            _currLineNum++;
            _isAtStartOfLine = true;
        }

        private void ConsumeLineBreakIfPresent()
        {
            Match('\r');
            Match('\n');

            _currLineNum++;
            _isAtStartOfLine = true;
        }

        private void AddImplicitFinalNewLine()
        {
            if (_scannedTokens.Count == 0)
            {
                return;
            }

            // Parsing expects the last statement to be terminated even when
            // the source file does not end with a newline character.
            TokenType lastTokenType = _scannedTokens[_scannedTokens.Count - 1].Type;
            
            if (lastTokenType != TokenType.NewLine && lastTokenType != TokenType.Dedent)
            {
                AddToken(TokenType.NewLine, string.Empty, null);
            }
        }

        private void AddRemainingDedents()
        {
            while (_currIndentLevels.Count > 1)
            {
                _currIndentLevels.Pop();
                AddToken(TokenType.Dedent, string.Empty, null);
            }
        }

        private void SkipComment()
        {
            while (!IsAtEnd() && !IsLineBreakStart(Peek()))
            {
                Advance();
            }
        }

        private void DecreaseGroupingDepth()
        {
            if (_groupingDepth == 0)
            {
                throw CreateError("Unexpected closing grouping delimiter.");
            }

            _groupingDepth--;
        }

        private bool Match(char expectedChar)
        {
            if (IsAtEnd())
            {
                return false;
            }

            if (_sourceCode[_currCharIndex] != expectedChar)
            {
                return false;
            }

            _currCharIndex++;
            return true;
        }

        private char Advance()
        {
            return _sourceCode[_currCharIndex++];
        }

        private char Peek()
        {
            if (IsAtEnd())
            {
                return '\0';
            }

            return _sourceCode[_currCharIndex];
        }

        private char PeekNext()
        {
            if (_currCharIndex + 1 >= _sourceCode.Length)
            {
                return '\0';
            }

            return _sourceCode[_currCharIndex + 1];
        }

        private bool IsAtEnd()
        {
            return _currCharIndex >= _sourceCode.Length;
        }

        private void AddToken(TokenType type)
        {
            AddToken(type, null);
        }

        private void AddToken(TokenType type, object literalValue)
        {
            string lexeme = _sourceCode.Substring(_lexemeStartIndex, _currCharIndex - _lexemeStartIndex);
            AddToken(type, lexeme, literalValue);
        }

        private void AddToken(TokenType type, string lexeme, object literalValue)
        {
            // Structural tokens do not prove that normal token scanning has begun on a
            // line; real tokens do, which prevents repeated indentation scans mid-line.

            switch (type)
            {
                case TokenType.NewLine:
                case TokenType.Indent:
                case TokenType.Dedent:
                case TokenType.EndOfFile:
                    break;

                default:
                    _isAtStartOfLine = false;
                    break;
            }
                
            _scannedTokens.Add(new Token(type, lexeme, literalValue, _currLineNum));
        }

        private ScannerException CreateError(string msg)
        {
            return new ScannerException(msg, _currLineNum);
        }

        private static bool IsDigit(char checkChar)
        {
            switch(checkChar)
            {
                case '0': case '1': case '2': case '3': case '4': 
                case '5': case '6': case '7': case '8': case '9':
                    return true;
            }
            
            return false;
        }

        private static bool IsIdentifierStart(char checkChar)
        {
            return (checkChar >= 'a' && checkChar <= 'z') || (checkChar >= 'A' && checkChar <= 'Z') || checkChar == '_';
        }

        private static bool IsIdentifierPart(char checkChar)
        {
            return IsIdentifierStart(checkChar) || IsDigit(checkChar);
        }

        private static bool IsLineBreakStart(char checkChar)
        {
            return checkChar == '\r' || checkChar == '\n';
        }
    }
}
