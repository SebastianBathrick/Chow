using System;

namespace Chow.Interpreter.Exceptions
{
    /// <summary>
    /// The base type for every error Chow reports about the source code it compiles and runs.
    /// Hosts can catch this type to handle any Chow error, or catch <see cref="CompileException"/>
    /// or <see cref="RuntimeException"/> to tell the two phases apart.
    /// </summary>
    public abstract class ChowException : Exception
    {
        readonly string _description;

        /// <summary>
        /// The Chow-facing name of the error, matching Python's exception names (e.g.
        /// <c>SyntaxError</c>, <c>TypeError</c>, or <c>ZeroDivisionError</c>).
        /// </summary>
        public string ErrorName { get; }

        /// <summary>
        /// The line of Chow source the error occurred on, or <c>null</c> when it did not originate
        /// from a line of Chow source, such as an error raised by a call made through the host API.
        /// </summary>
        public int? LineNumber { get; internal set; }

        /// <summary>
        /// The error formatted as <c>ErrorName: description on line N</c>, omitting the line suffix
        /// when <see cref="LineNumber"/> is <c>null</c>. An error with no description, such as
        /// <c>StopIteration</c>, is just its name, as Python prints it.
        /// </summary>
        // TODO: Add the line's source code to the exception message
        public override string Message
        {
            get
            {
                var error = string.IsNullOrEmpty(_description) ? ErrorName : $"{ErrorName}: {_description}";
                return LineNumber.HasValue ? $"{error} on line {LineNumber.Value}" : error;
            }
        }

        private protected ChowException(string errorName, string description, int? lineNumber)
        {
            ErrorName = errorName;
            _description = description;
            LineNumber = lineNumber;
        }
    }
}
