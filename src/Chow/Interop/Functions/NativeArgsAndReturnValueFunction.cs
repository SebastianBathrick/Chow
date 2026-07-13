using Chow.SourceData;

namespace Chow.Interop.Functions
{
    abstract class NativeArgsAndReturnValueFunction : InteropFunction
    {
        protected NativeArgsAndReturnValueFunction(
            SourceValue enclosingScope,
            Arity arity = Arity.None,
            int minArguments = 0,
            int maxArguments = 0)
            : base(enclosingScope, arity, minArguments, maxArguments)
        {
        }

        public sealed override InvokeOverload InvokeOverload => InvokeOverload.ArgsAndReturnValue;

        public abstract SourceValue Invoke(SourceValue[] args);
    }
}
