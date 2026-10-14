using Chow.Interpreter.Exceptions;

namespace Chow
{
    sealed class SyntaxException : CompileException
    {
        const string ExceptionAlias = "SyntaxError";

        public SyntaxException(string expected, int line)
            : this(line, $"{ExceptionAlias}: expected '{expected}'")
        {
        }

        SyntaxException(int line, string message) : base(message, line)
        {
        }

        /// <summary>
        /// Creates a syntax error whose message is <paramref name="message"/> as written, for errors
        /// that are not about a missing token (e.g. <c>'break' outside loop</c>).
        /// </summary>
        public static SyntaxException FromMessage(string message, int line)
        {
            return new SyntaxException(line, $"{ExceptionAlias}: {message}");
        }
    }
}
