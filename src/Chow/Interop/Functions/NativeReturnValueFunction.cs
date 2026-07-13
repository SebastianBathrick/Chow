using Chow.SourceData;

namespace Chow.Interop.Functions
{
    abstract class NativeReturnValueFunction : InteropFunction
    {
        protected NativeReturnValueFunction(
            SourceValue enclosingScope,
            Arity arity = Arity.None,
            int minArguments = 0,
            int maxArguments = 0)
            : base(enclosingScope, arity, minArguments, maxArguments)
        {
        }

        public sealed override InvokeOverload InvokeOverload => InvokeOverload.ReturnValue;

        public abstract SourceValue Invoke();
    }
}
