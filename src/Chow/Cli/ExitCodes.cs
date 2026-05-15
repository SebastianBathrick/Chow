namespace Chow.Cli
{
    /// <summary>
    /// Process exit codes returned by the Chow command-line application.
    /// </summary>
    internal static class ExitCodes
    {
        /// <summary>
        /// The command completed successfully.
        /// </summary>
        public const int Success = 0;

        /// <summary>
        /// Chow source execution failed with a non-fatal scanner, parser, semantic, or runtime error.
        /// </summary>
        public const int RuntimeError = 1;

        /// <summary>
        /// The command-line input could not be interpreted as a valid CLI request.
        /// </summary>
        public const int UsageError = 2;
    }
}
