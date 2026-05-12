using System;
using System.Collections.Generic;
using System.Text;
using Chow.Interpreter.State.Scopes;

namespace Chow.Interpreter.State.Values
{
    abstract class InteropClassObject
    {
        IScope Scope { get; }

        protected InteropClassObject()
        {
            Scope = new ClassScope();

            var initAttrs = GetInitAttributes();

            if (initAttrs == null)
            {
                return;
            }

            foreach (var attr in initAttrs)
            {
                Scope.AssignVariableValue(attr.name, attr.value);
            }
        }

        public TaggedUnion GetAttribute(string name)
        {
            return Scope.GetVariableValue(name);
        }

        protected virtual List<(string name, TaggedUnion value)> GetInitAttributes()
        {
            return null;
        }
    }
}
