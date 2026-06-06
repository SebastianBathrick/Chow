using Chow.Exceptions;

namespace Chow.Expressions
{
    static class EvaluatorDefinitions
    {
        public static IEvaluator Arithmetic { get; private set; }
        
        public static IEvaluator Comparison { get; private set; }
        
        public static IEvaluator Logic { get; private set; }
        
        public static void Create<T>(EvaluatorType type, T evaluatorObject) where T : IEvaluator
        {
            switch (type)
            {
                case EvaluatorType.Arithmetic:
                    Arithmetic = evaluatorObject;
                    return;
                case EvaluatorType.Comparison:
                    Comparison = evaluatorObject;
                    return;
                case EvaluatorType.Logic:
                    Logic = evaluatorObject;
                    return;
                default:
                    throw new UnreachableException(
                        nameof(Create), $"Invalid {nameof(EvaluatorType)} case: {type}");
            }
        }
    }
}
