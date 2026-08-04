using System;
using Chow.Bytecode;
using Chow.Code;
using Chow.Interpreter.Exceptions;

namespace Chow.SourceData
{
    sealed class SourceFunction : SourceObject
    {
        const string RepresentationFormat = "<function {0}>";

        public override DataType Type => DataType.Function;

        /// <summary>The compiled bytecode of the function body.</summary>
        public BytecodeChunk BytecodeChunk { get; }

        /// <summary>The scope active when <c>def</c> executed; used as the parent of the call's local scope.</summary>
        public Scope Enclosing { get; }

        /// <summary>The function name as written in source. Used for diagnostics and stack traces.</summary>
        public string Name { get; }

        /// <summary>Declared positional-parameter count. Used by the VM for arity checking at call sites.</summary>
        public int ParamCount { get; }

        /// <summary>
        /// The instance a method was looked up on, passed as the first argument when the call frame
        /// is pushed. Meaningful only when <see cref="HasReceiver"/> is <see langword="true"/>.
        /// </summary>
        public SourceValue Receiver { get; }

        /// <summary>
        /// Whether this is a bound method. Bound methods absorb their first declared parameter
        /// (<c>self</c>), so the VM expects one fewer argument at the call site.
        /// </summary>
        public bool HasReceiver { get; }

        /// <summary>Constructs a closure. All fields are readonly; closures are immutable once built.</summary>
        public SourceFunction(BytecodeChunk bytecodeChunk, Scope enclosing, string name, int paramCount)
        {
            BytecodeChunk = bytecodeChunk;
            Enclosing = enclosing;
            Name = name;
            ParamCount = paramCount;
        }

        SourceFunction(SourceFunction unbound, SourceValue receiver)
        {
            BytecodeChunk = unbound.BytecodeChunk;
            Enclosing = unbound.Enclosing;
            Name = unbound.Name;
            ParamCount = unbound.ParamCount;
            Receiver = receiver;
            HasReceiver = true;
        }

        /// <summary>
        /// Produces a bound method: the same closure with <paramref name="receiver"/> attached as
        /// its implicit first argument. Closures are immutable, so this returns a new instance
        /// sharing the original's bytecode and captured scope.
        /// </summary>
        public SourceFunction Bind(SourceValue receiver)
        {
            return new SourceFunction(this, receiver);
        }

        public override SourceValue GetAttribute(SourceValue name)
        {
            if (name.DataType != DataType.Str)
            {
                throw new InvalidOperationException(
                    $"{nameof(GetAttribute)} cannot be called with anything other "
                    + $"than a {nameof(SourceValue.DataType)} of {nameof(DataType.Str)}.");
            }

            if (name == SourceObjectConsts.ChunkAttribute)
            {
                return new SourceValue(BytecodeChunk);
            }

            if (name == SourceObjectConsts.EnclosingScopeAttribute)
            {
                return new SourceValue(Enclosing);
            }

            throw new AttributeException(
                DataTypeNames.GetTypeName(DataType.Str),
                name,
                -1);
        }

        public override string ToRepresentation()
        {
            return string.Format(RepresentationFormat, Name);
        }
    }
}
