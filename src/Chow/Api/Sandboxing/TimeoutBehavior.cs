using Chow.Sandboxing;

namespace Chow.Api.Sandboxing
{
    internal class TimeoutBehavior : InterpreterBehavior
    {
        float _limitMs;

        public TimeoutBehavior(long limitMs)
        {
            _limitMs = limitMs;
        }
    }
}
