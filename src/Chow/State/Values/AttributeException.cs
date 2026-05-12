using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Interpreter.State.Values
{
    internal class AttributeException : Exception
    {
        const string ERROR_LABEL = "Attribute Error";

        public AttributeException(string msg) : base($"{ERROR_LABEL}: {msg}")
        {

        }
    }
}
