using Chow.Api.Sandboxing;

namespace Chow.Sandboxing
{
    public abstract class InterpreterBehavior
    {
        public static InterpreterBehavior Timeout(long limitMs)
        {
            return new TimeoutBehavior(limitMs);
        }
    }
}
