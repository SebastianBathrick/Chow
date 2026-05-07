namespace Chow.Interpreter
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

        /// <summary>Equality comparison operator: <c>==</c>.</summary>
        EqualEqual,

        /// <summary>Inequality comparison operator: <c>!=</c>.</summary>
        BangEqual,

        /// <summary>Greater-than comparison operator: <c>&gt;</c>.</summary>
        Greater,

        /// <summary>Greater-than-or-equal comparison operator: <c>&gt;=</c>.</summary>
        GreaterEqual,

        /// <summary>Less-than comparison operator: <c>&lt;</c>.</summary>
        Less,

        /// <summary>Less-than-or-equal comparison operator: <c>&lt;=</c>.</summary>
        LessEqual,

        /// <summary>User-defined name or non-keyword identifier.</summary>
        Identifier,

        /// <summary>Keyword: <c>True</c>.</summary>
        True,

        /// <summary>Keyword: <c>False</c>.</summary>
        False,

        /// <summary>Keyword: <c>None</c>.</summary>
        None,

        /// <summary>Keyword: <c>and</c>.</summary>
        And,

        /// <summary>Keyword: <c>or</c>.</summary>
        Or,

        /// <summary>Keyword: <c>not</c>.</summary>
        Not,

        /// <summary>Keyword: <c>is</c>.</summary>
        Is,

        /// <summary>Keyword: <c>in</c>.</summary>
        In,

        /// <summary>Keyword: <c>def</c>.</summary>
        Def,

        /// <summary>Keyword: <c>return</c>.</summary>
        Return,

        /// <summary>Keyword: <c>class</c>.</summary>
        Class,

        /// <summary>Keyword: <c>with</c>.</summary>
        With,

        /// <summary>Keyword: <c>as</c>.</summary>
        As,

        /// <summary>Keyword: <c>global</c>.</summary>
        Global,

        /// <summary>Keyword: <c>if</c>.</summary>
        If,

        /// <summary>Keyword: <c>else</c>.</summary>
        Else,

        /// <summary>Keyword: <c>elif</c>.</summary>
        Elif,

        /// <summary>Keyword: <c>for</c>.</summary>
        For,

        /// <summary>Keyword: <c>while</c>.</summary>
        While,

        /// <summary>Keyword: <c>break</c>.</summary>
        Break,

        /// <summary>Keyword: <c>continue</c>.</summary>
        Continue,

        /// <summary>Keyword: <c>pass</c>.</summary>
        Pass,

        /// <summary>Keyword: <c>try</c>.</summary>
        Try,

        /// <summary>Keyword: <c>except</c>.</summary>
        Except,

        /// <summary>Keyword: <c>finally</c>.</summary>
        Finally,

        /// <summary>Keyword: <c>raise</c>.</summary>
        Raise,

        /// <summary>Keyword: <c>assert</c>.</summary>
        Assert,

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
