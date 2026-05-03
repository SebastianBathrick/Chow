using Chow.Values;
using System.Collections.Generic;

namespace Chow.Evaluation
{
    sealed class StackFrame
    {
        readonly Dictionary<string, TaggedUnion> _locals = new Dictionary<string, TaggedUnion>();

        public TaggedUnion this[string name] => _locals[name];

        public void SetLocal(string name, TaggedUnion value)
        {
            _locals[name] = value;
        }
    }
}