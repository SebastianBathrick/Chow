using Chow.SourceData;

namespace Chow.Interop.Functions
{
    abstract class NativeVoidFunction : InteropFunction
    {
        protected NativeVoidFunction(
            SourceValue enclosingScope,
            Arity arity = Arity.None,
            int minArguments = 0,
            int maxArguments = 0)
            : base(enclosingScope, arity, minArguments, maxArguments)
        {
        }

        public sealed override InvokeOverload InvokeOverload => InvokeOverload.Void;

        public abstract void Invoke();
    }
}
