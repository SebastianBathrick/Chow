using Chow.Interpreter.Compilation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Interpreter.Evaluation
{
    // Function that was defined in Chow source code, and not interoperated from the host language (C#).
    internal class Closure
    {
        Chunk _chunk;

        // This is readonly because the closure should capture the scope at the time of definition
        LocalScope _scope;
        string _name;
        int _paramCount;

        public Chunk Chunk => _chunk;

        public string Name => _name;

        public int ParamCount => _paramCount;

        public Closure(Chunk chunk, LocalScope scope, string name, int paramCount)
        {
            _chunk = chunk;
            _scope = scope;
            _name = name;
            _paramCount = paramCount;
        }

        public LocalScope CopyScope()
        {
            return new LocalScope(_scope);
        }
    }
}
