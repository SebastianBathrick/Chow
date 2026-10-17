using System;
using System.Collections.Generic;
using Chow.Bytecode;
using Chow.Interpreter.Exceptions;
using Chow.SourceData;

namespace Chow.Interpreter.VM
{
    /// <summary>
    /// Owns the module frame plus a stack of function-call frames, and exposes scope-aware
    /// variable operations that route through the active frame. Implements the LEGB lookup
    /// chain (L → E* → G) via <see cref="Scope.Parent"/> walking; assignments always
    /// land in the current frame's scope (Python local-by-default).
    /// </summary>
    class CallStack
    {
        const string ArityErrorFormat = "{0}() takes {1} positional arguments but {2} were given";
        const string RecursionErrorFormat = "maximum recursion depth of {0} exceeded";

        // The Processor attaches the current instruction's line to errors raised here.
        const int NoLineNumber = -1;

        // The call frames on this thread held by call stacks that are waiting on a host call. A host
        // callback that calls back into Chow gets a new call stack, which starts from this count so
        // the recursion limit spans the whole chain.
        [ThreadStatic]
        static int t_hostCallerDepth;

        readonly StackFrame _moduleLvl;
        readonly Stack<StackFrame> _callFrames;
        readonly int _maxRecursionDepth;

        // Fixed for this call stack's lifetime: t_hostCallerDepth only changes while this call stack
        // is itself waiting on a host call, and is restored before it resumes.
        readonly int _callerDepth;

        // Cached active frame (module frame or top call frame); kept in sync by
        // EnterFunctionCall/ExitFunctionCall so per-instruction reads avoid Count/PeekType.

        /// <summary>
        /// The bytecodeChunk currently being executed (function bytecodeChunk if inside a call,
        /// module bytecodeChunk otherwise).
        /// </summary>
        public BytecodeChunk CurrentBytecodeChunk => CurrFrame.BytecodeChunk;

        /// <summary>The instruction at the current frame's pointer.</summary>
        public Instruction CurrentInstr => CurrFrame.CurrentInstr;

        /// <summary>True while the current frame has instructions remaining.</summary>
        public bool IsInstructionToExecute => CurrFrame.IsInstrToRun;

        /// <summary>The current frame's scope. Captured by <c>PushNewSourceFunction</c> at runtime.</summary>
        public Scope CurrentScope => CurrFrame.Scope;

        /// <summary>
        /// The module-level scope (the bottom of every LEGB chain). Future <c>global</c>-targeted ops
        /// route directly here.
        /// </summary>
        public Scope ModuleScope => _moduleLvl.Scope;

        /// <summary>Source line number associated with the current frame's pointer.</summary>
        public int CurrentLineNum => CurrFrame.CurrentLineNum;

        StackFrame CurrFrame
        {
            get;
            set;
        }

        /// <summary>Creates a call stack rooted at a single module frame.</summary>
        /// <param name="moduleBytecodeChunk">The compiled bytecode for the module being executed.</param>
        /// <param name="moduleScope">
        /// The module scope to operate against; persists across
        /// <c>ChowState.ProcessInstructions</c> calls.
        /// </param>
        public CallStack(BytecodeChunk moduleBytecodeChunk, Scope moduleScope)
        {
            _moduleLvl = new StackFrame(moduleBytecodeChunk, moduleScope);
            _callFrames = new Stack<StackFrame>();
            CurrFrame = _moduleLvl;

            // A host call into a callee that carries no scope (e.g. a class) has no module scope,
            // and a scope that was never run has no settings, so both fall back to the defaults.
            _maxRecursionDepth = (moduleScope?.RunSettings ?? default(ChowRunSettings)).MaxRecursionDepth;
            _callerDepth = t_hostCallerDepth;
        }

        /// <summary>
        /// Binds <paramref name="name"/> to <paramref name="value"/> in the current frame's scope.
        /// At module level this writes to the module scope; inside a function call it writes to
        /// that call's local scope (Python local-by-default). The <c>global</c>/<c>nonlocal</c>
        /// targeted variants <see cref="AssignToGlobal"/> and <see cref="AssignToNonlocal"/>
        /// rebind enclosing or module names.
        /// </summary>
        public void AssignVariableValue(string name, ref SourceValue value)
        {
            CurrFrame.Scope.AssignVariableValue(name, ref value);
        }

        /// <summary>
        /// Binds <paramref name="name"/> to <paramref name="value"/> directly in the module scope,
        /// bypassing the current frame's local scope. Used by the <c>global</c>-targeted opcodes.
        /// </summary>
        public void AssignToGlobal(string name, ref SourceValue value)
        {
            ModuleScope.AssignVariableValue(name, ref value);
        }

        /// <summary>
        /// Reads <paramref name="name"/> directly from the module scope, bypassing any local or
        /// enclosing scopes. Used by the <c>global</c>-targeted read opcode.
        /// </summary>
        public bool TryGetGlobal(string name, out SourceValue value)
        {
            return ModuleScope.TryGetVariableValue(name, out value);
        }

        /// <summary>
        /// Binds <paramref name="name"/> to <paramref name="value"/> in the nearest enclosing
        /// function scope that already defines <paramref name="name"/>. The module scope is
        /// excluded from the walk. Throws <see cref="UndefinedNameException"/> if no such scope
        /// exists, which happens when the enclosing function has not bound the name yet.
        /// </summary>
        public void AssignToNonlocal(string name, ref SourceValue value)
        {
            var scope = FindNonlocalScope(name);
            scope.AssignVariableValue(name, ref value);
        }

        /// <summary>
        /// Reads <paramref name="name"/> from the nearest enclosing function scope that defines
        /// it. The module scope is excluded from the walk. Throws <see cref="UndefinedNameException"/>
        /// if no such scope exists, which happens when the enclosing function has not bound the name
        /// yet.
        /// </summary>
        public SourceValue GetNonlocal(string name)
        {
            for (var s = CurrFrame.Scope.Parent; s != null && !ReferenceEquals(s, _moduleLvl.Scope); s = s.Parent)
            {
                if (s.TryGetVariableValue(name, out var value))
                {
                    return value;
                }
            }

            throw new UndefinedNameException(name, NoLineNumber);
        }

        // Walks ParentOrNull from CurrFrame.Scope upward, stopping before the module scope
        // (compared by reference against _moduleLvl.Scope), and returns the first scope that
        // defines `name`. Throws UndefinedNameException if none does.
        Scope FindNonlocalScope(string name)
        {
            for (var s = CurrFrame.Scope.Parent; s != null && !ReferenceEquals(s, _moduleLvl.Scope); s = s.Parent)
            {
                if (s.TryGetVariableValue(name, out _))
                {
                    return s;
                }
            }

            throw new UndefinedNameException(name, NoLineNumber);
        }

        /// <summary>
        /// Resolves <paramref name="name"/> by walking Local → Enclosing(s) → Module with a single
        /// dictionary probe per scope. Returns false if the name is unbound along the entire chain.
        /// </summary>
        public bool TryGetVariableValue(string name, out SourceValue value)
        {
            for (var s = CurrFrame.Scope; s != null; s = s.Parent)
            {
                if (s.TryGetVariableValue(name, out value))
                {
                    return true;
                }
            }

            value = SourceValue.None;
            return false;
        }

        /// <summary>
        /// Resolves <paramref name="name"/> by walking Local → Enclosing(s) → Module and returns
        /// the first binding found. A missing name surfaces as <see cref="KeyNotFoundException"/>
        /// (NameError translation is the VM's responsibility).
        /// </summary>
        public SourceValue GetVariableValue(string name)
        {
            return TryGetVariableValue(name, out var value) 
                ? value
                // Contract violation: callers must verify the name is defined first.
                // KeyNotFoundException here surfaces the bug; NameError translation belongs to the VM.
                : CurrFrame.Scope.GetVariableValue(name);

        }

        /// <summary>Advances the current frame's instruction pointer by one.</summary>
        public void MoveToNextInstruction()
        {
            CurrFrame.MoveToNextInstr();
        }

        /// <summary>Sets the current frame's instruction pointer.</summary>
        public void JumpToInstr(int instrIdx)
        {
            CurrFrame.JumpToInstr(instrIdx);
        }

        /// <summary>
        /// Throws a <see cref="DataTypeException"/> when <paramref name="argCount"/> does not match
        /// the number of arguments <paramref name="func"/> expects. Must be called before the
        /// caller's instruction pointer moves past the call, so the error reports the call's line.
        /// </summary>
        public static void ThrowIfArityMismatch(ISourceObject func, int argCount)
        {
            var sourceFunc = (SourceFunction)func;

            // A bound method absorbs its first parameter (self) from the receiver, so the call site
            // is expected to supply one fewer argument than the method declares.
            var expectedArgCount = sourceFunc.HasReceiver
                ? sourceFunc.ParamCount - 1 : sourceFunc.ParamCount;

            if (argCount != expectedArgCount)
            {
                throw new DataTypeException(
                    string.Format(ArityErrorFormat, sourceFunc.Name, expectedArgCount, argCount));
            }
        }

        /// <summary>
        /// Throws a <see cref="RecursionException"/> when entering another function call would
        /// exceed the recursion limit. Like <see cref="ThrowIfArityMismatch"/>, it must be called
        /// before the caller's instruction pointer moves past the call.
        /// </summary>
        public void ThrowIfRecursionLimitReached()
        {
            // Module frames aren't counted, so a limit of N allows N nested calls.
            if (_maxRecursionDepth != ChowRunSettings.NoRecursionLimit
                && _callerDepth + _callFrames.Count >= _maxRecursionDepth)
            {
                throw new RecursionException(string.Format(RecursionErrorFormat, _maxRecursionDepth));
            }
        }

        /// <summary>
        /// Marks this call stack as waiting on a host call, so any Chow code the host calls back into
        /// counts this call stack's frames toward its recursion limit. Must be paired with
        /// <see cref="ExitHostCall"/> in a <c>finally</c> block.
        /// </summary>
        public void EnterHostCall()
        {
            t_hostCallerDepth = _callerDepth + _callFrames.Count;
        }

        /// <summary>Undoes <see cref="EnterHostCall"/> once the host call has returned or thrown.</summary>
        public void ExitHostCall()
        {
            t_hostCallerDepth = _callerDepth;
        }

        /// <summary>
        /// Pushes a new function frame for <paramref name="func"/>, whose arity must already have
        /// been checked with <see cref="ThrowIfArityMismatch"/>. A fresh <see cref="Scope"/> is
        /// allocated with its parent set to the closure's captured enclosing scope, becoming the L of
        /// LEGB for the duration of the call.
        /// </summary>
        public void EnterFunctionCall(ISourceObject func)
        {
            var sourceFunc = (SourceFunction)func;
            var frameScope = new Scope(sourceFunc.Enclosing);
            var newFrame = new StackFrame(sourceFunc.BytecodeChunk, frameScope);
            _callFrames.Push(newFrame);
            CurrFrame = newFrame;
        }

        /// <summary>
        /// Marks the frame entered most recently as a constructor call, so its caller receives
        /// <paramref name="instance"/> in place of the frame's return value.
        /// </summary>
        public void SetConstructionResult(SourceValue instance)
        {
            CurrFrame.SetConstructionResult(instance);
        }

        /// <summary>
        /// Pops the current function frame and returns it, so the caller can inspect what the
        /// completed call should yield. The frame's local scope is dropped (kept alive only if a
        /// nested closure captured it).
        /// </summary>
        public StackFrame ExitFunctionCall()
        {
            var completedFrame = _callFrames.Pop();
            CurrFrame = _callFrames.Count == 0 ? _moduleLvl : _callFrames.Peek();

            return completedFrame;
        }
    }
}
