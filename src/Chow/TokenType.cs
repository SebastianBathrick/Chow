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

        /// <summary>Division operator: <c>/</c>.</summary>
        Slash,

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

        /// <summary>Add-and-assign operator: <c>+=</c>.</summary>
        PlusEqual,

        /// <summary>Subtract-and-assign operator: <c>-=</c>.</summary>
        MinusEqual,

        /// <summary>Multiply-and-assign operator: <c>*=</c>.</summary>
        StarEqual,

        /// <summary>Divide-and-assign operator: <c>/=</c>.</summary>
        SlashEqual,

        /// <summary>Modulo-and-assign operator: <c>%=</c>.</summary>
        PercentEqual,

        /// <summary>User-defined name or non-keyword identifier.</summary>
        Identifier,

        /// <summary>String literal.</summary>
        String,

        Integer,

        Float,

        /// <summary><c>and</c> keyword.</summary>
        And,

        /// <summary><c>class</c> keyword.</summary>
        Class,

        /// <summary><c>def</c> keyword.</summary>
        Def,

        /// <summary><c>elif</c> keyword.</summary>
        Elif,

        /// <summary><c>else</c> keyword.</summary>
        Else,

        /// <summary><c>False</c> or <c>false</c> boolean literal keyword.</summary>
        False,

        /// <summary><c>for</c> keyword.</summary>
        For,

        /// <summary><c>if</c> keyword.</summary>
        If,

        /// <summary><c>in</c> keyword.</summary>
        In,

        /// <summary><c>None</c> or <c>none</c> null literal keyword.</summary>
        None,

        /// <summary><c>not</c> keyword.</summary>
        Not,

        /// <summary><c>or</c> keyword.</summary>
        Or,

        /// <summary><c>pass</c> keyword.</summary>
        Pass,

        /// <summary><c>return</c> keyword.</summary>
        Return,

        /// <summary><c>True</c> or <c>true</c> boolean literal keyword.</summary>
        True,

        /// <summary><c>while</c> keyword.</summary>
        While,

        /// <summary>Significant logical line break.</summary>
        Newline,

        /// <summary>Increase in leading indentation at the start of a logical line.</summary>
        Indent,

        /// <summary>Decrease in leading indentation back to a previous indentation level.</summary>
        Dedent,

        /// <summary>End-of-file sentinel token appended after all source code is scanned.</summary>
        EndOfFile
    }
}
