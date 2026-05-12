using System;
using System.Collections.Generic;
using Chow.Interpreter.Exceptions;
using System.Collections.ObjectModel;
using System.Text;
using System.Reflection;
using System.Linq;

namespace Chow.Interpreter.State.Values
{
    abstract class InteropClassObject
    {
        // These methods are delegates that are defined build-time, and child classes should not create new delegates runtime
        readonly Dictionary<string, Delegate> _methodMap;
        readonly Dictionary<string, TaggedUnion> _fieldMap;
        readonly bool _isAttrsReadOnly;

        public abstract string ClassName { get; }

        protected InteropClassObject(bool isAttrsReadOnly = false)
        {
            _methodMap = new Dictionary<string, Delegate>();
            _fieldMap = new Dictionary<string, TaggedUnion>();
            _isAttrsReadOnly = isAttrsReadOnly;

            var initMethods = GetInitMethods();

            foreach (var initMethod in initMethods)
            {
                _methodMap.Add(initMethod.name, initMethod.methodDelegate);
            }

            var initFields = GetInitFields();

            foreach (var initField in initFields)
            {
                _fieldMap.Add(initField.name, initField.fieldValue);
            }
        }

        protected abstract (string name, Delegate methodDelegate)[] GetInitMethods();

        protected abstract (string name, TaggedUnion fieldValue)[] GetInitFields();

        public TaggedUnion GetAttributeValue(string name)
        {
            ValidateAttributeExists(name);

            if (!_methodMap.ContainsKey(name))
            {
                return _fieldMap[name];
            }
            var methodDelegate = _methodMap[name];

            // There's logic to invoke this elsewhere
            return TaggedUnion.CreateWithValue(methodDelegate);
        }

        public void ReassignAttribute(string name, TaggedUnion value)
        {
            ValidateAttributeExists(name);

            // Call ContainsKey even though in the validation method we did that for clarity
            if (_methodMap.ContainsKey(name))
            {
                // Set to null, because it is being overridden to not be an interop method defined in THIS class
                // This still could be an interop method, but at this point, the TaggedUnion is of an unknown type
                _methodMap[name] = null;
            }

            // If it was a method, it is now a field because it is no longer associated with the build-time delegate
            _fieldMap[name] = value;
        }

        public TaggedUnion CallMethod(string name, params object[] args)
        {
            ValidateMethodExists(name);

            var method = _methodMap[name];
            var methodInfo = method.GetMethodInfo();
            var paramCount = methodInfo.GetParameters().Length;
            var isVoid = methodInfo.ReturnType == typeof(void);
            TaggedUnion returnVal = TaggedUnion.None;

            if (args == null && paramCount > 0)
            {
                var pluralOrNon = paramCount == 1 ? "argument" : "arguments";
                throw new TypeException($"{ClassName}.{name} takes exactly {paramCount} {pluralOrNon} ({paramCount} given)");
            }

            if (paramCount == 0)
            {
                if (isVoid)
                {
                    // As with any other void function, methods will return None, so don't reassign returnVal
                    method.DynamicInvoke();
                }
                else
                {
                    var returnObj = method.DynamicInvoke();

                    if (returnObj != null)
                    {
                        returnVal = TaggedUnion.CreateWithValue(returnObj);
                    }
                }
            }
            else
            {
                if (isVoid)
                {
                    method.DynamicInvoke(args);
                }
                else
                {
                    var returnObj = method.DynamicInvoke(args);

                    if (returnObj != null)
                    {
                        returnVal = TaggedUnion.CreateWithValue(returnObj);
                    }
                }
            }

            return returnVal;
        }

        void ValidateAttributeExists(string name)
        {
            // Interop class objects CAN'T have attributes declared at runtime.
            // However, attributes can be reassigned if the read-only flag is set to false
            if (!_fieldMap.ContainsKey(name) && !_methodMap.ContainsKey(name))
            {
                throw new AttributeException($"'{ClassName}' object has no attribute '{name}'");
            }

            if (_isAttrsReadOnly)
            {
                throw new AttributeException($"'{ClassName}' object attribute '{name}' is read-only");
            }
        }

        void ValidateMethodExists(string name)
        {
            if (!_methodMap.ContainsKey(name))
            {
                throw new AttributeException($"'{ClassName}' object has no method '{name}'");
            }
        }

        void ValidateArgumentCount(string methodName, bool isNull, int expectedCount, int givenCount)
        {
            if (isNull && expectedCount == 0)
            {
                return;
            }

            if (expectedCount != givenCount)
            {
                var pluralOrNon = expectedCount == 1 ? "argument" : "arguments";
                throw new TypeException($"{ClassName}.{methodName} takes exactly {expectedCount} {pluralOrNon} ({givenCount} given)");
            }
        }
}
