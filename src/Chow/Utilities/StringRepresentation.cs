using System.Globalization;
using System.Text;

namespace Chow.Code
{
    /// <summary>Produces Python's <c>repr()</c> of a string, following CPython's <c>unicode_repr</c>.</summary>
    static class StringRepresentation
    {
        const char SingleQuote = '\'';
        const char DoubleQuote = '"';
        const char Backslash = '\\';
        const char FirstPrintableAscii = ' ';
        const char AsciiDelete = '\x7f';
        const int LastLatin1CodePoint = 0xff;
        const int LastBmpCodePoint = 0xffff;

        public static string Format(string value)
        {
            // Single quotes unless that would need escaping and double quotes wouldn't.
            var quote = value.IndexOf(SingleQuote) >= 0 && value.IndexOf(DoubleQuote) < 0
                ? DoubleQuote
                : SingleQuote;

            var sb = new StringBuilder(value.Length + 2);
            sb.Append(quote);

            for (var i = 0; i < value.Length; i++)
            {
                var ch = value[i];

                if (ch == quote || ch == Backslash)
                {
                    sb.Append(Backslash).Append(ch);
                    continue;
                }

                switch (ch)
                {
                    case '\t':
                        sb.Append("\\t");
                        continue;
                    case '\n':
                        sb.Append("\\n");
                        continue;
                    case '\r':
                        sb.Append("\\r");
                        continue;
                }

                if (ch < FirstPrintableAscii || ch == AsciiDelete)
                {
                    AppendEscape(sb, ch);
                    continue;
                }

                if (ch < AsciiDelete)
                {
                    sb.Append(ch);
                    continue;
                }

                var isPair = char.IsHighSurrogate(ch) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]);
                var codePoint = isPair ? char.ConvertToUtf32(ch, value[i + 1]) : ch;

                if (IsPrintable(CharUnicodeInfo.GetUnicodeCategory(value, i)))
                {
                    sb.Append(ch);

                    if (isPair)
                    {
                        sb.Append(value[i + 1]);
                    }
                }
                else
                {
                    AppendEscape(sb, codePoint);
                }

                if (isPair)
                {
                    i++;
                }
            }

            sb.Append(quote);
            return sb.ToString();
        }

        static void AppendEscape(StringBuilder sb, int codePoint)
        {
            if (codePoint <= LastLatin1CodePoint)
            {
                sb.Append("\\x").Append(codePoint.ToString("x2", CultureInfo.InvariantCulture));
            }
            else if (codePoint <= LastBmpCodePoint)
            {
                sb.Append("\\u").Append(codePoint.ToString("x4", CultureInfo.InvariantCulture));
            }
            else
            {
                sb.Append("\\U").Append(codePoint.ToString("x8", CultureInfo.InvariantCulture));
            }
        }

        // Python's str.isprintable: everything except the "Other" and "Separator" categories.
        static bool IsPrintable(UnicodeCategory category)
        {
            switch (category)
            {
                case UnicodeCategory.Control:
                case UnicodeCategory.Format:
                case UnicodeCategory.Surrogate:
                case UnicodeCategory.PrivateUse:
                case UnicodeCategory.OtherNotAssigned:
                case UnicodeCategory.LineSeparator:
                case UnicodeCategory.ParagraphSeparator:
                case UnicodeCategory.SpaceSeparator:
                    return false;
                default:
                    return true;
            }
        }
    }
}
