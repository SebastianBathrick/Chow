namespace Chow
{
    /// <summary>
    /// Identifies the scanner category for a token.
    /// </summary>
    enum TokenType
    {
        /// <summary>Opening parenthesis: <c>(</c>.</summary>
        LeftParenthesis,

        /// <summary>Closing parenthesis: <c>)</c>.</summary>
        RightParenthesis,

        /// <summary>Opening square bracket: <c>[</c>.</summary>
        LeftBracket,

        /// <summary>Closing square bracket: <c>]</c>.</summary>
        RightBracket,

        /// <summary>Opening curly brace: <c>{</c>.</summary>
        LeftCurlyBrace,

        /// <summary>Closing curly brace: <c>}</c>.</summary>
        RightCurlyBrace,

        /// <summary>Comma separator: <c>,</c>.</summary>
        Comma,

        /// <summary>Dot punctuation: <c>.</c>.</summary>
        Dot,

        /// <summary>Colon punctuation, commonly used before an indented block: <c>:</c>.</summary>
        Colon,

        /// <summary>Addition operator: <c>+</c>.</summary>
        Plus,

        /// <summary>Subtraction or unary negation operator: <c>-</c>.</summary>
        Minus,

        /// <summary>Multiplication operator: <c>*</c>.</summary>
        Star,

        /// <summary>Exponentiation operator: <c>**</c>.</summary>
        StarStar,

        /// <summary>Division operator: <c>/</c>.</summary>
        Slash,

        /// <summary>Floor division operator: <c>//</c>.</summary>
        SlashSlash,

        /// <summary>Modulo operator: <c>%</c>.</summary>
        Percent,

        /// <summary>Assignment operator: <c>=</c>.</summary>
        Equal,

        /// <summary>Greater-than comparison operator: <c>&gt;</c>.</summary>
        Greater,

        /// <summary>Less-than comparison operator: <c>&lt;</c>.</summary>
        Less,

        /// <summary>User-defined name or non-keyword identifier.</summary>
        Identifier,

        /// <summary>String literal.</summary>
        String,

        Integer,

        Float,

        /// <summary>Significant logical line break.</summary>
        Newline,

        /// <summary>Increase in leading indentation at the start of a logical line.</summary>
        Indent,

        /// <summary>Decrease in leading indentation back to a previous indentation level.</summary>
        Dedent,

        /// <summary>End-of-file sentinel token appended after all source code is scanned.</summary>
        EndOfCode
    }
}
