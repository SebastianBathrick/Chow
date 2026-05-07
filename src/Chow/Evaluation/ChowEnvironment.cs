using Chow.Interpreter.Values;
using System.Collections.Generic;

namespace Chow.Interpreter.Evaluation
{
    internal class ChowEnvironment
    {
        const string SCOPE_BOUNDARY_ELEMENT = "<SCOPE_BOUNDARY>";

        private Stack<string> _varNameStack = new Stack<string>();
        private Dictionary<string, TaggedUnion> _varValMap = new Dictionary<string, TaggedUnion>();

        public ChowEnvironment()
        {
            // Push a boundary element to mark the bottom of the top-level scope (which will never be popped)
            EnterScope();
        }

        public bool IsVariable(string name)
        {
            return _varValMap.ContainsKey(name);
        }

        public void DeclareVariable(string name)
        {
            _varNameStack.Push(name);
        }

        public void EnterScope()
        {
            _varNameStack.Push(SCOPE_BOUNDARY_ELEMENT);
        }

        public void ExitScope()
        {
            // Pop the name of the variable declared last OR the boundary element if no variables were declared in the current scope
            string poppedName = _varNameStack.Pop();

            // Pop until the boundary element has been popped (either popped or is below the name of the first variable in the scope)
            while (poppedName != SCOPE_BOUNDARY_ELEMENT)
            {
                // Remove variable name and its assigned value from the map
                _varValMap.Remove(poppedName);

                // Pop another variable name OR the scope boundary element if there's no more variables left in the scope
                poppedName = _varNameStack.Pop();
            }
        }

        public void AssignVariableValue(string name, TaggedUnion value)
        {
            _varValMap[name] = value;
        }

        public TaggedUnion GetVariableValue(string name)
        {
            return _varValMap[name];
        }
    }
}
