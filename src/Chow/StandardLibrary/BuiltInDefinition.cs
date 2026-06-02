using System;
namespace Chow.StandardLibrary
{
    readonly struct BuiltInDefinition
    {
        const int ARGUMENT_COUNT_UNDEFINED = 0;
        
        public string Name { get; }
        public int MinimumArguments { get; }
        public int MaximumArguments { get; }

        public bool HasParameters => MaximumArguments > ARGUMENT_COUNT_UNDEFINED;
        
        public Func<ChowValue[], ChowValue>  ValueReturnDelegateWithParams { get; }
        public Func<ChowValue> ValueReturnDelegate { get; }
        public Action VoidDelegate { get;  }
        public Action<ChowValue[]> VoidDelegateWithParams { get; }
        
        
        public bool IsVoid { get; }
        
        /// <summary>
        /// Defines a built-in function that accepts arguments and returns a <see cref="ChowValue"/>.
        /// </summary>
        /// <param name="name">The source-language name the built-in is bound to in the module scope.</param>
        /// <param name="minimumArguments">The smallest number of arguments accepted at call time.</param>
        /// <param name="maximumArguments">The largest number of arguments accepted at call time.</param>
        /// <param name="valueReturnDelegateWithParams">The delegate invoked with the call's arguments;
        /// its return value is pushed onto the VM stack as the call result.</param>
        public BuiltInDefinition(
            string name,
            int minimumArguments,
            int maximumArguments,
            Func<ChowValue[], ChowValue> valueReturnDelegateWithParams)
        {
            Name = name;
            MinimumArguments = minimumArguments;
            MaximumArguments = maximumArguments;
            IsVoid = false;
            ValueReturnDelegateWithParams = valueReturnDelegateWithParams;
            ValueReturnDelegate = null;
            VoidDelegate = null;
            VoidDelegateWithParams = null;
        }

        /// <summary>
        /// Defines a built-in function that accepts arguments and produces no return value;
        /// the call result is implicitly <see cref="ChowValue.None"/>.
        /// </summary>
        /// <param name="name">The source-language name the built-in is bound to in the module scope.</param>
        /// <param name="minimumArguments">The smallest number of arguments accepted at call time.</param>
        /// <param name="maximumArguments">The largest number of arguments accepted at call time.</param>
        /// <param name="voidDelegateWithParams">The delegate invoked with the call's arguments for
        /// its side effects.</param>
        public BuiltInDefinition(
            string name,
            int minimumArguments,
            int maximumArguments,
            Action<ChowValue[]> voidDelegateWithParams)
        {
            Name = name;
            MinimumArguments = minimumArguments;
            MaximumArguments = maximumArguments;
            IsVoid = true;
            ValueReturnDelegateWithParams = null;
            ValueReturnDelegate = null;
            VoidDelegate = null;
            VoidDelegateWithParams = voidDelegateWithParams;
        }

        /// <summary>
        /// Defines a built-in function that accepts arguments and returns a <see cref="ChowValue"/>.
        /// </summary>
        /// <param name="name">The source-language name the built-in is bound to in the module scope.</param>
        /// <param name="valueReturnDelegateWithParams">The delegate invoked with the call's arguments;
        /// its return value is pushed onto the VM stack as the call result.</param>
        /// <param name="minimumArguments">The smallest number of arguments accepted at call time.</param>
        /// <param name="maximumArguments">The largest number of arguments accepted at call time.</param>
        public BuiltInDefinition(
            string name,
            Func<ChowValue[], ChowValue> valueReturnDelegateWithParams,
            int minimumArguments,
            int maximumArguments)
        {
            Name = name;
            MinimumArguments = minimumArguments;
            MaximumArguments = maximumArguments;
            IsVoid = false;
            ValueReturnDelegateWithParams = valueReturnDelegateWithParams;
            ValueReturnDelegate = null;
            VoidDelegate = null;
            VoidDelegateWithParams = null;
        }

        /// <summary>
        /// Defines a built-in function that accepts arguments and produces no return value;
        /// the call result is implicitly <see cref="ChowValue.None"/>.
        /// </summary>
        /// <param name="name">The source-language name the built-in is bound to in the module scope.</param>
        /// <param name="voidDelegateWithParams">The delegate invoked with the call's arguments for
        /// its side effects.</param>
        /// <param name="minimumArguments">The smallest number of arguments accepted at call time.</param>
        /// <param name="maximumArguments">The largest number of arguments accepted at call time.</param>
        public BuiltInDefinition(
            string name,
            Action<ChowValue[]> voidDelegateWithParams,
            int minimumArguments,
            int maximumArguments)
        {
            Name = name;
            MinimumArguments = minimumArguments;
            MaximumArguments = maximumArguments;
            IsVoid = true;
            ValueReturnDelegateWithParams = null;
            ValueReturnDelegate = null;
            VoidDelegate = null;
            VoidDelegateWithParams = voidDelegateWithParams;
        }

        /// <summary>
        /// Defines a built-in function that takes no arguments and returns a <see cref="ChowValue"/>.
        /// </summary>
        /// <param name="name">The source-language name the built-in is bound to in the module scope.</param>
        /// <param name="valueReturnDelegate">The delegate invoked with no arguments; its return
        /// value is pushed onto the VM stack as the call result.</param>
        public BuiltInDefinition(
            string name,
            Func<ChowValue> valueReturnDelegate)
        {
            Name = name;
            MinimumArguments = ARGUMENT_COUNT_UNDEFINED;
            MaximumArguments = ARGUMENT_COUNT_UNDEFINED;
            IsVoid = false;
            ValueReturnDelegateWithParams = null;
            ValueReturnDelegate = valueReturnDelegate;
            VoidDelegate = null;
            VoidDelegateWithParams = null;
        }

        /// <summary>
        /// Defines a built-in function that takes no arguments and produces no return value;
        /// the call result is implicitly <see cref="ChowValue.None"/>.
        /// </summary>
        /// <param name="name">The source-language name the built-in is bound to in the module scope.</param>
        /// <param name="voidDelegate">The delegate invoked with no arguments for its side effects.</param>
        public BuiltInDefinition(
            string name,
            Action voidDelegate)
        {
            Name = name;
            MinimumArguments = ARGUMENT_COUNT_UNDEFINED;
            MaximumArguments = ARGUMENT_COUNT_UNDEFINED;
            IsVoid = true;
            ValueReturnDelegateWithParams = null;
            ValueReturnDelegate = null;
            VoidDelegate = voidDelegate;
            VoidDelegateWithParams = null;
        }
    }
}