namespace Chow.Cli
{
    /// <summary>
    /// Centralizes CLI exception handling decisions.
    /// </summary>
    internal static class ExceptionPolicy
    {
        /// <summary>
        /// Determines whether the exception represents a host failure that the CLI should not swallow.
        /// </summary>
        /// <param name="ex">The exception to inspect.</param>
        /// <returns><see langword="true"/> when the exception should escape the CLI error handler.</returns>
        public static bool IsFatal(Exception ex)
        {
            return ex is OutOfMemoryException
                || ex is StackOverflowException
                || ex is AccessViolationException
                || ex is AppDomainUnloadedException
                || ex is BadImageFormatException
                || ex is CannotUnloadAppDomainException;
        }

        /// <summary>
        /// Writes a user-facing exception message to standard error.
        /// </summary>
        /// <param name="ex">The exception to report.</param>
        public static void WriteError(Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
        }
    }
}
