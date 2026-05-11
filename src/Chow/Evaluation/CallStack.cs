using Chow.Interpreter.Compilation;
using Chow.Interpreter.Values.Internal;
using System.Collections.Generic;

namespace Chow.Interpreter.Evaluation
{
    internal class CallStack
    {
        StackFrame _moduleLvl;
        Stack<StackFrame> _callFrames;

        public Chunk CurrentChunk => CurrFrame.Chunk;

        public bool IsModuleLevel => _callFrames.Count == 0;

        protected StackFrame CurrFrame
        {
            get
            {
                if (_callFrames.Count == 0)
                {
                    return _moduleLvl;
                }

                return _callFrames.Peek();
            }
        }

        public CallStack(Chunk moduleChunk, LocalScope moduleScope)
        {
            _moduleLvl = new StackFrame(moduleChunk, moduleScope);
            _callFrames = new Stack<StackFrame>();
        }

        // Bare assignment binds in the current frame's scope (Python local-by-default).
        // Module-level code's current frame IS _moduleLvl, so module-level assignments still
        // land in the module scope.
        public void AssignVariableValue(string name, TaggedUnion value)
        {
            CurrFrame.Scope.AssignVariableValue(name, value);
        }

        // Lookup walks locals first, then falls back to the module scope (LEGB without enclosing/builtin yet).
        public bool IsVariableDefined(string name)
        {
            if (CurrFrame.Scope.IsVariableDefined(name))
            {
                return true;
            }

            if (IsModuleLevel)
            {
                return false;
            }

            return _moduleLvl.Scope.IsVariableDefined(name);
        }

        public TaggedUnion GetVariableValue(string name)
        {
            if (CurrFrame.Scope.IsVariableDefined(name))
            {
                return CurrFrame.Scope.GetVariableValue(name);
            }

            return _moduleLvl.Scope.GetVariableValue(name);
        }

        public void EnterNestedScope()
        {
            CurrFrame.Scope.EnterNestedScope();
        }

        public void ExitNestedScope()
        {
            CurrFrame.Scope.ExitNestedScope();
        }

        public void EnterFunctionCall(Closure func)
        {
            StackFrame newFrame = new StackFrame(funcChunk);
            _callFrames.Push(newFrame);
        }

        public void ExitFunctionCall()
        {
            _callFrames.Pop();
        }
    }
}
