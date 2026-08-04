using System.Collections.Generic;
using Chow.SourceData;

namespace Chow.Bytecode
{
    /// <summary>
    /// Compile-time-only representation of a class, mirroring <see cref="FunctionDefinition"/>.
    /// Stored as a constant in the parent <c>BytecodeChunk</c> and consumed by the
    /// <c>PushNewSourceClass</c> op at runtime, which combines this template with the currently
    /// active scope and the evaluated class-variable values to produce a real
    /// <see cref="SourceClass"/>.
    /// </summary>
    sealed class ClassDefinition
    {
        /// <summary>The class name as written in source.</summary>
        public string Name { get; }

        /// <summary>The templates for the methods declared in the class body.</summary>
        public FunctionDefinition[] Methods { get; }

        /// <summary>
        /// The class-variable names in declaration order, matching the order their evaluated values
        /// are handed to <see cref="MakeClass"/>.
        /// </summary>
        public string[] ClassVariableNames { get; }

        public ClassDefinition(string name, FunctionDefinition[] methods, string[] classVariableNames)
        {
            Name = name;
            Methods = methods;
            ClassVariableNames = classVariableNames;
        }

        /// <summary>
        /// Combines this template with the scope active at <c>class</c> time and the evaluated
        /// class-variable values to produce the runtime class value. Methods close over
        /// <paramref name="enclosing"/> rather than over the class, matching how a nested <c>def</c>
        /// captures its defining scope.
        /// </summary>
        /// <param name="enclosing">The scope the class declaration executed in.</param>
        /// <param name="classVariableValues">
        /// The class-variable values, in the same order as <see cref="ClassVariableNames"/>.
        /// </param>
        public ISourceObject MakeClass(Scope enclosing, SourceValue[] classVariableValues)
        {
            var attributes = new Dictionary<string, SourceValue>(
                Methods.Length + ClassVariableNames.Length);

            foreach (var method in Methods)
            {
                attributes[method.Name] = new SourceValue(method.MakeClosure(enclosing));
            }

            // Applied after the methods, so a class variable sharing a method's name wins regardless
            // of which came first in the source.
            for (var i = 0; i < ClassVariableNames.Length; i++)
            {
                attributes[ClassVariableNames[i]] = classVariableValues[i];
            }

            return new SourceClass(Name, attributes);
        }
    }
}
