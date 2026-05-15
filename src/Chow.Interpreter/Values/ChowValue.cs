namespace Chow.Interpreter.Values
{
    /// <summary>
    /// Base class for all values returned by the Chow interpreter API. Use <see cref="AsType{TDataType}"/> to
    /// extract the underlying value, or pattern-match on a concrete subclass (<see cref="ChowInt"/>,
    /// <see cref="ChowFloat"/>, <see cref="ChowBool"/>, <see cref="ChowStr"/>, <see cref="ChowList"/>,
    /// <see cref="ChowDict"/>, <see cref="ChowObject"/>, <see cref="ChowNone"/>).
    /// </summary>
    public abstract class ChowValue
    {
        /// <summary>Gets the singleton <c>None</c> value.</summary>
        public static ChowValue None => ChowNone.Instance;

        /// <summary>
        /// Returns <see langword="true"/> if this value is <c>None</c>. Equivalent to
        /// <c>value == ChowValue.None</c>.
        /// </summary>
        public bool IsNone => this == None;

        // TODO: Refactor AsType<T>() and IsType<T>() to use a nullable type
        /// <summary>
        /// Extracts the underlying value as <typeparamref name="TDataType"/>.
        /// </summary>
        /// <typeparam name="TDataType">The target value type.</typeparam>
        /// <returns>The converted value.</returns>
        /// <exception cref="InvalidCastException">The conversion is not supported by this value type.</exception>
        public abstract TDataType AsType<TDataType>() where TDataType : struct;

        /// <summary>
        /// Returns <see langword="true"/> if this value's native type is <typeparamref name="TDataType"/>.
        /// </summary>
        /// <typeparam name="TDataType">The type to test against.</typeparam>
        public abstract bool IsType<TDataType>() where TDataType : struct;

        /// <summary>Returns a string representation of the value.</summary>
        public abstract override string ToString();
    }
}
