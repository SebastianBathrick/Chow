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

        /// <summary>Constructs a closure. All fields are readonly; closures are immutable once built.</summary>
        public SourceFunction(BytecodeChunk bytecodeChunk, Scope enclosing, string name, int paramCount)
        {
            BytecodeChunk = bytecodeChunk;
            Enclosing = enclosing;
            Name = name;
            ParamCount = paramCount;
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
