using Chow.Interpreter.State.Values;
using Chow.Interpreter.Values;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Chow.Interpreter
{

    enum BuiltInType
    {
        Print, Input
    }

    public class BuiltIns
    {
        bool _isDirty;
        private List<(string name, object value)> _dirtyValues;

        internal bool IsDirty => _isDirty;

        internal BuiltIns()
        {
            _isDirty = true;
            // Add all builtins to the dirty values so that they get added into the chow module during first execution
        }

        public void SetActive(bool isActive, params BuiltInType[] builtInTypes)
        {
            // Null or empty builtins means we're setting the state of all builtins, otherwise we're setting the state of the specified builtins
            // When set from active to inactive
        }

        // Clients never use the actual name, just the enum, it helps the client to know which builtins are actually available in Chow, plus we can add xml to each enum value describing the function
        public void SetValue(BuiltInType builtInType, object value)
        {
            // For now, we'll support Closures, ChowFunctions, Delegates (the same ones that are supported by TaggedUnion)
        }

        // Chow module will handle conversions from object to the appropriate type, so we can just return object here
        internal List<(string name, object value)> GetDirtyValues()
        {
            // Make sure to replace _dirtyValues with null so there aren't two references
        }

        internal void MarkAsNotDirty()
        {
            _isDirty = false;
        }
    }
}
