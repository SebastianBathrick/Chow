using System;

namespace Chow
{
    /// <summary>
    /// Settings for a <see cref="ChowEngine"/> <c>Run</c> call. The default value
    /// (<c>default(ChowRunSettings)</c> or <c>new ChowRunSettings()</c>) includes the built-in
    /// functions and limits recursion to <see cref="DefaultMaxRecursionDepth"/>. Use the
    /// <c>With</c> methods to change a setting.
    /// <para>
    /// A scope's first run decides whether it has the built-in functions, and later runs on the
    /// same scope must make the same choice. The recursion limit can change between runs, and calls
    /// the host makes into the scope's functions use the limit from its most recent run.
    /// </para>
    /// </summary>
    public readonly struct ChowRunSettings
    {
        /// <summary>The recursion limit used when none is set, matching CPython's default.</summary>
        public const int DefaultMaxRecursionDepth = 1000;

        /// <summary>The <see cref="MaxRecursionDepth"/> value that disables the recursion limit.</summary>
        public const int NoRecursionLimit = 0;

        // Null until set, so default(ChowRunSettings) means DefaultMaxRecursionDepth while an
        // explicit 0 means no limit.
        readonly int? _maxRecursionDepth;

        /// <summary>
        /// Whether the built-in functions are left out of the scope. <see langword="false"/> by
        /// default, so the built-in functions are available.
        /// </summary>
        public bool ExcludeBuiltIns { get; }

        /// <summary>
        /// The maximum number of nested Chow function calls before a <c>RecursionError</c> is
        /// raised, or <see cref="NoRecursionLimit"/> for no limit. Defaults to
        /// <see cref="DefaultMaxRecursionDepth"/>.
        /// </summary>
        public int MaxRecursionDepth => _maxRecursionDepth ?? DefaultMaxRecursionDepth;

        ChowRunSettings(int? maxRecursionDepth, bool excludeBuiltIns)
        {
            _maxRecursionDepth = maxRecursionDepth;
            ExcludeBuiltIns = excludeBuiltIns;
        }

        /// <summary>Returns a copy of these settings with a different recursion limit.</summary>
        /// <param name="maxRecursionDepth">The maximum number of nested Chow function calls, or
        /// <see cref="NoRecursionLimit"/> for no limit.</param>
        /// <returns>The new settings.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxRecursionDepth"/> is
        /// negative.</exception>
        public ChowRunSettings WithMaxRecursionDepth(int maxRecursionDepth)
        {
            if (maxRecursionDepth < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxRecursionDepth), maxRecursionDepth, "The recursion limit cannot be negative.");
            }

            return new ChowRunSettings(maxRecursionDepth, ExcludeBuiltIns);
        }

        /// <summary>Returns a copy of these settings that includes or excludes the built-in functions.</summary>
        /// <param name="excludeBuiltIns">Whether the built-in functions are left out of the scope.</param>
        /// <returns>The new settings.</returns>
        public ChowRunSettings WithExcludeBuiltIns(bool excludeBuiltIns = true)
        {
            return new ChowRunSettings(_maxRecursionDepth, excludeBuiltIns);
        }
    }
}
