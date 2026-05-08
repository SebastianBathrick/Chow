using Chow.Interpreter.Values;
using System.Collections.Generic;

namespace Chow.Interpreter.Evaluation
{
    internal class ChowEnvironment
    {
        const string SCOPE_BOUNDARY_ELEMENT = "<SCOPE_BOUNDARY>";
        const int TOP_LVL_SCOPE_DEPTH = 0;

        private Stack<string> _varNameStack;
        private Dictionary<string, TaggedUnion> _varValMap;
        private int _scopeDepthLvl;

        public bool IsTopLevelScope => _scopeDepthLvl == TOP_LVL_SCOPE_DEPTH;

        public ChowEnvironment()
        {
            _varValMap = new Dictionary<string, TaggedUnion>();
            _scopeDepthLvl = TOP_LVL_SCOPE_DEPTH;

            // The bottom of the stack represents the top-level scope (which will never be popped)
            _varNameStack = new Stack<string>();
            _varNameStack.Push(SCOPE_BOUNDARY_ELEMENT);
        }

        public bool IsVariableDefined(string name)
        {
            return _varValMap.ContainsKey(name);
        }

        public void DeclareVariable(string name)
        {
            _varNameStack.Push(name);
        }

        public void EnterScope()
        {
            _scopeDepthLvl++;
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

            _scopeDepthLvl--;
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
