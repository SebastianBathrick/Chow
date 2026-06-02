using System.Collections.Generic;
namespace Chow.Interpreter.DataTypes
{
    sealed class FStringTokenPayload
    {
        public List<string> StringParts { get; }

        public List<string> ExprSourceParts { get; }

        public FStringTokenPayload(List<string> stringParts, List<string> exprSourceParts)
        {
            StringParts = stringParts;
            ExprSourceParts = exprSourceParts;
        }
    }
}
