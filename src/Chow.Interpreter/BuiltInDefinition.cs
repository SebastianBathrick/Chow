using System;
using Chow.Interpreter.Exceptions;
using Chow.Interpreter.Values;
namespace Chow.Interpreter
{
    readonly struct BuiltInDefinition
    {
        public string Name { get; }
        public Func<ChowValue[], ChowValue> Implementation { get; }
        public int MinimumArguments { get; }
        public int MaximumArguments { get; }
        public bool HasParameters => MaximumArguments > 0;
        public bool IsVariadic => MaximumArguments != MinimumArguments;


        public BuiltInDefinition(string name, Func<ChowValue[], ChowValue> implementation, int minimumArguments, int maximumArguments)
        {
            Name = name;
            Implementation = implementation;
            MinimumArguments = minimumArguments;
            MaximumArguments = maximumArguments;
        }

        public BuiltInDefinition(string name, Func<ChowValue[], ChowValue> implementation, int reqArgCount)
        {
            Name = name;
            Implementation = implementation;
            MinimumArguments = reqArgCount;
            MaximumArguments = reqArgCount;
        }

        /// <summary>
        /// Throws <see cref="TypeException"/> if <paramref name="args"/> does not satisfy this definition's
        /// <see cref="MinimumArguments"/>/<see cref="MaximumArguments"/> range.
        /// </summary>
        public void RequireArity(ChowValue[] args)
        {
            var actual = args?.Length ?? 0;
            if (actual < MinimumArguments || actual > MaximumArguments)
            {
                throw new TypeException($"{Name}() expected {FormatExpectedArity()} arguments, got {actual}");
            }
        }

        /// <summary>
        /// Throws <see cref="ArgumentException"/> if this definition's arity is not fixed at <paramref name="expectedArity"/>.
        /// Used to reject typed-delegate overloads whose implicit arity is incompatible with the built-in's contract.
        /// </summary>
        public void RequireFixedArity(int expectedArity, string delegateLabel)
        {
            if (MinimumArguments != expectedArity || MaximumArguments != expectedArity)
            {
                throw new ArgumentException(
                    $"Cannot set '{Name}' ({FormatExpectedArity()} arg{(MinimumArguments == 1 && !IsVariadic ? "" : "s")}) using a {delegateLabel} delegate ({expectedArity} arg{(expectedArity == 1 ? "" : "s")})");
            }
        }

        /// <summary>Wraps <paramref name="impl"/> so each invocation enforces this definition's arity.</summary>
        public Func<ChowValue[], ChowValue> WrapWithArityCheck(Func<ChowValue[], ChowValue> impl)
        {
            var def = this;
            return args =>
            {
                def.RequireArity(args);
                return impl(args ?? Array.Empty<ChowValue>());
            };
        }

        string FormatExpectedArity()
        {
            return IsVariadic ? $"{MinimumArguments} to {MaximumArguments}" : MinimumArguments.ToString();
        }
    }
}
