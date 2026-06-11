namespace Chow.Tokens
{
    /// <summary>
    /// Identifies the scanner category for a token.
    /// </summary>
    enum TokenType : byte
    {
        /// <summary>Library defined token that is not from Chow source code.</summary>
        EmptyToken,
        
        /// <summary>Opening parenthesis: <c>(</c>.</summary>
        SymbolLeftParen,

        /// <summary>Closing parenthesis: <c>)</c>.</summary>
        SymbolRightParen,

        /// <summary>Opening square bracket: <c>[</c>.</summary>
        SymbolLeftBracket,

        /// <summary>Closing square bracket: <c>]</c>.</summary>
        SymbolRightBracket,

        /// <summary>Opening curly brace: <c>{</c>.</summary>
        SymbolLeftCurly,

        /// <summary>Closing curly brace: <c>}</c>.</summary>
        SymbolRightCurly,

        /// <summary>Comma separator: <c>,</c>.</summary>
        SymbolComma,

        /// <summary>Dot punctuation: <c>.</c>.</summary>
        SymbolDot,

        /// <summary>Colon punctuation, commonly used before an indented block: <c>:</c>.</summary>
        SymbolColon,

        /// <summary>Addition operator: <c>+</c>.</summary>
        SymbolPlus,

        /// <summary>Subtraction or unary negation operator: <c>-</c>.</summary>
        SymbolMinus,

        /// <summary>Multiplication operator: <c>*</c>.</summary>
        SymbolMultiply,

        /// <summary>Exponentiation operator: <c>**</c>.</summary>
        SymbolExponent,

        /// <summary>Division operator: <c>/</c>.</summary>
        SymbolDivide,

        /// <summary>Floor division operator: <c>//</c>.</summary>
        SymbolFloorDivide,

        /// <summary>Modulo operator: <c>%</c>.</summary>
        SymbolPercent,

        /// <summary>Assignment operator: <c>=</c>.</summary>
        SymbolAssign,

        /// <summary>Equality comparison operator: <c>==</c>.</summary>
        SymbolEqualTo,

        /// <summary>Inequality comparison operator: <c>!=</c>.</summary>
        SymbolNotEqual,

        /// <summary>Greater-than comparison operator: <c>&gt;</c>.</summary>
        SymbolGreater,

        /// <summary>Greater-than-or-equal comparison operator: <c>&gt;=</c>.</summary>
        SymbolGreaterEqual,

        /// <summary>Less-than comparison operator: <c>&lt;</c>.</summary>
        SymbolLess,

        /// <summary>Less-than-or-equal comparison operator: <c>&lt;=</c>.</summary>
        SymbolLessEqual,

        /// <summary>Pipe operator, used for dictionary merge: <c>|</c>.</summary>
        SymbolPipe,

        /// <summary>User-defined name or non-keyword identifier.</summary>
        Name,

        /// <summary>Keyword: <c>True</c>.</summary>
        KeywordTrue,

        /// <summary>Keyword: <c>False</c>.</summary>
        KeywordFalse,

        /// <summary>Keyword: <c>None</c>.</summary>
        KeywordNone,

        /// <summary>Keyword: <c>and</c>.</summary>
        KeywordAnd,

        /// <summary>Keyword: <c>or</c>.</summary>
        KeywordOr,

        /// <summary>Keyword: <c>not</c>.</summary>
        KeywordNot,

        /// <summary>Keyword: <c>is</c>.</summary>
        KeywordIs,

        /// <summary>Keyword: <c>in</c>.</summary>
        KeywordIn,

        /// <summary>Keyword: <c>def</c>.</summary>
        KeywordDef,

        /// <summary>Keyword: <c>return</c>.</summary>
        KeywordReturn,

        /// <summary>Keyword: <c>class</c>.</summary>
        KeywordClass,

        /// <summary>Keyword: <c>with</c>.</summary>
        KeywordWith,

        /// <summary>Keyword: <c>as</c>.</summary>
        KeywordAs,

        /// <summary>Keyword: <c>global</c>.</summary>
        KeywordGlobal,

        /// <summary>Keyword: <c>nonlocal</c>.</summary>
        KeywordNonlocal,

        /// <summary>Keyword: <c>if</c>.</summary>
        KeywordIf,

        /// <summary>Keyword: <c>else</c>.</summary>
        KeywordElse,

        /// <summary>Keyword: <c>elif</c>.</summary>
        KeywordElif,

        /// <summary>Keyword: <c>for</c>.</summary>
        KeywordFor,

        /// <summary>Keyword: <c>while</c>.</summary>
        KeywordWhile,

        /// <summary>Keyword: <c>break</c>.</summary>
        KeywordBreak,

        /// <summary>Keyword: <c>continue</c>.</summary>
        KeywordContinue,

        /// <summary>Keyword: <c>pass</c>.</summary>
        KeywordPass,

        /// <summary>Keyword: <c>try</c>.</summary>
        KeywordTry,

        /// <summary>Keyword: <c>except</c>.</summary>
        KeywordExcept,

        /// <summary>Keyword: <c>finally</c>.</summary>
        KeywordFinally,

        /// <summary>Keyword: <c>raise</c>.</summary>
        KeywordRaise,

        /// <summary>Keyword: <c>assert</c>.</summary>
        KeywordAssert,

        /// <summary>String literal.</summary>
        LiteralStr,

        /// <summary>Formatted string literal (f-string).</summary>
        LiteralFString,

        LiteralInt,

        LiteralFloat,

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
