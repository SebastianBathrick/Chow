using Chow.Interpreter.Exceptions;

namespace Chow.SourceData
{
    sealed class SourceScope : SourceObject
    {
        bool _hasExprResult;
        
        public override DataType Type => DataType.Scope;

        public override bool HasLength => true;

        public override int Length => InternalInternalScope.Count - (_hasExprResult ? 1 : 0);

        internal Scope InternalInternalScope
        {
            get;
        }

        public SourceScope(Scope internalScope)
        {
            InternalInternalScope = internalScope;
            SetItem(SourceObjectConsts.ScopeExpressionName, SourceValue.None);
        }
        

        public override SourceValue GetItem(SourceValue key)
        {
            return key.DataType != DataType.Str 
                ? throw new DataTypeException($"The key '{key}' is not a str") 
                : InternalInternalScope.GetVariableValue(key.ToString());

        }

        public override void SetItem(SourceValue key, SourceValue value)
        {
            if (key.DataType != DataType.Str)
            {
                throw new DataTypeException($"The key '{key}' is not a str");
            }

            if (key == SourceObjectConsts.ScopeExpressionName)
            {
                _hasExprResult = true;
            }
            
            InternalInternalScope.AssignVariableValue(key.ToString(), ref value);
        }

        public override SourceValue GetAttribute(SourceValue name)
        {
            return name == SourceObjectConsts.ScopeWrappedScopeAttributeName 
                ? new SourceValue(InternalInternalScope) 
                : base.GetAttribute(name);

        }
    }
}
