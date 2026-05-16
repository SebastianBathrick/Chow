using Chow.Interpreter.Bytecode;
using Chow.Interpreter.State.Scopes;
using Chow.Interpreter.State.Values;
using System.Collections.Generic;

namespace Chow.Interpreter.State.Stack
{
    /// <summary>
    /// Owns the module frame plus a stack of function-call frames, and exposes scope-aware
    /// variable operations that route through the active frame. Implements the LEGB lookup
    /// chain (L → E* → G) via <see cref="Scope.ParentOrNull"/> walking; assignments always
    /// land in the current frame's scope (Python local-by-default).
    /// </summary>
    class CallStack
    {
        readonly StackFrame _moduleLvl;
        readonly Stack<StackFrame> _callFrames;

        /// <summary>The chunk currently being executed (function chunk if inside a call, module chunk otherwise).</summary>
        public Chunk CurrentChunk => CurrFrame.Chunk;

        /// <summary>The instruction at the current frame's pointer.</summary>
        public Instruction CurrentInstr => CurrFrame.CurrentInstr;

        /// <summary>True while the current frame has instructions remaining.</summary>
        public bool IsInstrToRun => CurrFrame.IsInstrToRun;

        /// <summary>The current frame's scope. Captured by <c>PushNewClosureFromTemplate</c> at runtime.</summary>
        public Scope CurrentScope => CurrFrame.Scope;

        /// <summary>The module-level scope (the bottom of every LEGB chain). Future <c>global</c>-targeted ops route directly here.</summary>
        public Scope ModuleScope => _moduleLvl.Scope;

        /// <summary>Source line number associated with the current frame's pointer.</summary>
        public int CurrentLineNum => CurrFrame.CurrentLineNum;

        /// <summary>True when no function call is active and execution is in the module frame.</summary>
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

        /// <summary>Creates a call stack rooted at a single module frame.</summary>
        /// <param name="moduleChunk">The compiled bytecode for the module being executed.</param>
        /// <param name="moduleScope">The module scope to operate against; persists across <c>ChowModule.Execute</c> calls.</param>
        public CallStack(Chunk moduleChunk, Scope moduleScope)
        {
            _moduleLvl = new StackFrame(moduleChunk, moduleScope);
            _callFrames = new Stack<StackFrame>();
        }

        /// <summary>
        /// Binds <paramref name="name"/> to <paramref name="value"/> in the current frame's scope.
        /// At module level this writes to the module scope; inside a function call it writes to
        /// that call's local scope (Python local-by-default). The <c>global</c>/<c>nonlocal</c>
        /// targeted variants <see cref="AssignToGlobal"/> and <see cref="AssignToNonlocal"/>
        /// rebind enclosing or module names.
        /// </summary>
        public void AssignVariableValue(string name, ChowValue value)
        {
            CurrFrame.Scope.AssignVariableValue(name, value);
        }

        /// <summary>
        /// Binds <paramref name="name"/> to <paramref name="value"/> directly in the module scope,
        /// bypassing the current frame's local scope. Used by the <c>global</c>-targeted opcodes.
        /// </summary>
        public void AssignToGlobal(string name, ChowValue value)
        {
            ModuleScope.AssignVariableValue(name, value);
        }

        /// <summary>
        /// Reads <paramref name="name"/> directly from the module scope, bypassing any local or
        /// enclosing scopes. Used by the <c>global</c>-targeted read opcode.
        /// </summary>
        public ChowValue GetGlobal(string name)
        {
            return ModuleScope.GetVariableValue(name);
        }

        /// <summary>True if <paramref name="name"/> is bound in the module scope.</summary>
        public bool IsGlobalDefined(string name)
        {
            return ModuleScope.IsVariableDefined(name);
        }

        /// <summary>
        /// Binds <paramref name="name"/> to <paramref name="value"/> in the nearest enclosing
        /// function scope that already defines <paramref name="name"/>. The module scope is
        /// excluded from the walk. Throws <see cref="KeyNotFoundException"/> if no such scope
        /// exists; semantic analysis is expected to prevent this at compile time.
        /// </summary>
        public void AssignToNonlocal(string name, ChowValue value)
        {
            var scope = FindNonlocalScope(name);
            scope.AssignVariableValue(name, value);
        }

        /// <summary>
        /// Reads <paramref name="name"/> from the nearest enclosing function scope that defines
        /// it. The module scope is excluded from the walk. Throws <see cref="KeyNotFoundException"/>
        /// if no such scope exists; semantic analysis is expected to prevent this at compile time.
        /// </summary>
        public ChowValue GetNonlocal(string name)
        {
            var scope = FindNonlocalScope(name);
            return scope.GetVariableValue(name);
        }

        // Walks ParentOrNull from CurrFrame.Scope upward, stopping before the module scope
        // (compared by reference against _moduleLvl.Scope), and returns the first scope that
        // defines `name`. Throws KeyNotFoundException if none does.
        Scope FindNonlocalScope(string name)
        {
            for (var s = CurrFrame.Scope.ParentOrNull; s != null && !ReferenceEquals(s, _moduleLvl.Scope); s = s.ParentOrNull)
            {
                if (s.IsVariableDefined(name))
                {
                    return s;
                }
            }

            throw new KeyNotFoundException($"No enclosing scope binds nonlocal '{name}'");
        }

        /// <summary>
        /// True if <paramref name="name"/> resolves anywhere along the LEGB chain from the current
        /// frame upward. At module level this is a single-scope probe.
        /// </summary>
        public bool IsVariableDefined(string name)
        {
            for (var s = CurrFrame.Scope; s != null; s = s.ParentOrNull)
            {
                if (s.IsVariableDefined(name))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Resolves <paramref name="name"/> by walking Local → Enclosing(s) → Module and returns
        /// the first binding found. Callers must check <see cref="IsVariableDefined"/> first; a
        /// missing name surfaces as <see cref="KeyNotFoundException"/> (NameError translation is
        /// the VM's responsibility).
        /// </summary>
        public ChowValue GetVariableValue(string name)
        {
            for (var s = CurrFrame.Scope; s != null; s = s.ParentOrNull)
            {
                if (s.IsVariableDefined(name))
                {
                    return s.GetVariableValue(name);
                }
            }

            // Contract violation: callers must check IsVariableDefined first.
            // KeyNotFoundException here surfaces the bug; NameError translation belongs to the VM.
            return CurrFrame.Scope.GetVariableValue(name);
        }

        /// <summary>Advances the current frame's instruction pointer by one.</summary>
        public void MoveToNextInstr()
        {
            CurrFrame.MoveToNextInstr();
        }

        /// <summary>Sets the current frame's instruction pointer.</summary>
        public void JumpToInstr(int instrIdx)
        {
            CurrFrame.JumpToInstr(instrIdx);
        }

        /// <summary>
        /// Pushes a new function frame for <paramref name="func"/>. A fresh <see cref="Scope"/>
        /// is allocated with its parent set to the closure's captured enclosing scope, becoming the
        /// L of LEGB for the duration of the call.
        /// </summary>
        public void EnterFunctionCall(Closure func)
        {
            var frameScope = new Scope(func.Enclosing);
            var newFrame = new StackFrame(func.Chunk, frameScope);
            _callFrames.Push(newFrame);
        }

        /// <summary>Pops the current function frame. Its local scope is dropped (kept alive only if a nested closure captured it).</summary>
        public void ExitFunctionCall()
        {
            _callFrames.Pop();
        }
    }
}
