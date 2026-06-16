using System;

namespace Chow
{
    public class UnreachableException : Exception
    {
        public UnreachableException(string functionName, string extraInfo = "")
            : base(
                $"An instruction executed that was thought to be unreachable in {functionName}."
                + extraInfo)
        {
        }
    }
}
