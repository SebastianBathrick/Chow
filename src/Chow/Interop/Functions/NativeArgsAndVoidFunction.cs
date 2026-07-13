using Chow.SourceData;

namespace Chow.Interop.Functions
{
    abstract class NativeArgsAndVoidFunction : InteropFunction
    {
        protected NativeArgsAndVoidFunction(
            SourceValue enclosingScope,
            Arity arity = Arity.None,
            int minArguments = 0,
            int maxArguments = 0)
            : base(enclosingScope, arity, minArguments, maxArguments)
        {
        }

        public sealed override InvokeOverload InvokeOverload => InvokeOverload.ArgsAndVoid;

        public abstract void Invoke(SourceValue[] args);
    }
}
