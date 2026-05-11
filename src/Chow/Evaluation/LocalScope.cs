using Chow.Interpreter.Values.Internal;
using System.Collections.Generic;

namespace Chow.Interpreter.Evaluation
{
    // TODO: Might add back environment
    internal class LocalScope
    {
        // Note that no identifier can be named "<SCOPE_BOUNDARY_ELEMENT>", because identifiers cannot start with a "<"
        const string SCOPE_BOUNDARY_ELEMENT = "<SCOPE_BOUNDARY>";
        const int OUTERMOST_SCOPE_DEPTH = 0;

        private Stack<string> _varNames;
        private Dictionary<string, TaggedUnion> _varMap;
        private int _scopeDepth;

        public bool IsOutermostDepth => _scopeDepth == OUTERMOST_SCOPE_DEPTH;

        public LocalScope()
        {
            _varMap = new Dictionary<string, TaggedUnion>();
            _scopeDepth = OUTERMOST_SCOPE_DEPTH;

            // The bottom of the stack represents the outermost scope (which will never be popped)
            _varNames = new Stack<string>();
            _varNames.Push(SCOPE_BOUNDARY_ELEMENT);
        }

        public LocalScope(LocalScope orig)
        {
            _varMap = new Dictionary<string, TaggedUnion>(orig._varMap);
            _scopeDepth = OUTERMOST_SCOPE_DEPTH;
            // The bottom of the stack represents the outermost scope (which will never be popped)
            _varNames = new Stack<string>(orig._varNames);
        }

        public bool IsVariableDefined(string name)
        {
            return _varMap.ContainsKey(name);
        }

        public void EnterNestedScope()
        {
            _scopeDepth++;
            _varNames.Push(SCOPE_BOUNDARY_ELEMENT);
        }

        public void ExitNestedScope()
        {
            // Pop the name of the variable declared last OR the boundary element if no variables were declared in the current scope
            string popName = _varNames.Pop();

            // Pop until the boundary element has been popped (either popped or is below the name of the first variable in the scope)
            while (popName != SCOPE_BOUNDARY_ELEMENT)
            {
                // Remove variable name and its assigned value from the map
                _varMap.Remove(popName);

                // Pop another variable name OR the scope boundary element if there's no more variables left in the scope
                popName = _varNames.Pop();
            }

            _scopeDepth--;
        }

        public void AssignVariableValue(string name, TaggedUnion value)
        {
            // First-time assignment also declares: track the name in the current scope
            // so it gets removed from the value map when the scope exits.
            if (!_varMap.ContainsKey(name))
            {
                _varNames.Push(name);
            }

            _varMap[name] = value;
        }

        public TaggedUnion GetVariableValue(string name)
        {
            return _varMap[name];
        }
    }
}
