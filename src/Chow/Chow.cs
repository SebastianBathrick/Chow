using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Interpreter
{
    internal class Chow
    {
        // This is intended for executing single-use or utility Chow code that does not need to save the instances state
        public ChowValue Run(string sourceCode)
        {
            ChowInstance instance = new ChowInstance();
            return instance.Run(sourceCode);
        }
    }
}
