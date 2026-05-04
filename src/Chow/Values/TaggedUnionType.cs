using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Interpreter.Values
{
    internal enum TaggedUnionType
    {
        Empty,
        None,
        Integer,
        Float,
        String
    }
}
