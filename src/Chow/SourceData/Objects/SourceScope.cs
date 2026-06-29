using Chow.VM;

namespace Chow.SourceData
{
    sealed class SourceScope : SourceObject
    {
        readonly Scope _scope;

        public override DataType Type => DataType.Scope;

        public override bool HasLength => true;

        public override int Length => _scope.Count;

        internal Scope InternalScope => _scope;

        public SourceScope(Scope scope)
        {
            _scope = scope;
            SetItem(SourceObjectConsts.ScopeExpressionName, SourceValue.None);
        }
        

        public override SourceValue GetItem(SourceValue key)
        {
            if (key.DataType != DataType.Str)
            {
                throw new DataTypeException($"The key '{key}' is not a str");
            }
            
            return _scope.GetVariableValue(key.ToString());
        }

        public override void SetItem(SourceValue key, SourceValue value)
        {
            if (key.DataType != DataType.Str)
            {
                throw new DataTypeException($"The key '{key}' is not a str");
            }
            
            _scope.AssignVariableValue(key.ToString(), ref value);
        }

        public override SourceValue GetAttribute(SourceValue name)
        {
            return name == SourceObjectConsts.ScopeWrappedScopeAttributeName 
                ? new SourceValue(_scope) 
                : base.GetAttribute(name);

        }
    }
}
