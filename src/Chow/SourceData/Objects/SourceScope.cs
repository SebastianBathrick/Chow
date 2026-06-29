using Chow.VM;

namespace Chow.SourceData
{
    sealed class SourceScope : SourceObject
    {

        public override DataType Type => DataType.Scope;

        public override bool HasLength => true;

        public override int Length => InternalInternalScope.Count;

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
