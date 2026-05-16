namespace Chow.Repl
{
    /// <summary>
    /// Defines the prompts used by the interactive Chow line editor.
    /// </summary>
    readonly struct PromptStyle
    {
        /// <summary>
        /// Initializes a prompt style for first-line and continuation-line input.
        /// </summary>
        /// <param name="startIndicator">Prompt printed before the first input line.</param>
        /// <param name="continuationIndicator">Prompt printed before subsequent input lines.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="startIndicator"/> or <paramref name="continuationIndicator"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">The indicators have different lengths.</exception>
        public PromptStyle(string startIndicator, string continuationIndicator)
        {
            if (startIndicator == null)
            {
                throw new ArgumentNullException(nameof(startIndicator));
            }

            if (continuationIndicator == null)
            {
                throw new ArgumentNullException(nameof(continuationIndicator));
            }

            if (startIndicator.Length != continuationIndicator.Length)
            {
                throw new ArgumentException("Prompt indicators must have the same length.", nameof(continuationIndicator));
            }

            StartIndicator = startIndicator;
            ContinuationIndicator = continuationIndicator;
        }

        /// <summary>
        /// Gets the prompt printed before the first line of a submission.
        /// </summary>
        public string StartIndicator { get; }

        /// <summary>
        /// Gets the prompt printed before continuation lines in a multi-line submission.
        /// </summary>
        public string ContinuationIndicator { get; }

        /// <summary>
        /// Gets the shared prompt width used to align cursor movement across input lines.
        /// </summary>
        public int IndicatorLength => StartIndicator.Length;

        /// <summary>
        /// Gets the default Chow REPL prompt style.
        /// </summary>
        public static PromptStyle Default => new PromptStyle(">>> ", "... ");
    }
}
