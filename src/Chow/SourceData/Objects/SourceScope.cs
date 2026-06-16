using Chow.VM;

namespace Chow.SourceData
{
    sealed class SourceScope : SourceObject
    {
        readonly Scope _scope;
        SourceValue _expressionStatementResult;

        public override DataType Type => DataType.Scope;

        public override bool HasLength => true;

        public override int Length => _scope.Count;

        internal Scope InternalScope => _scope;


        public SourceScope(Scope scope, SourceValue expressionStatementResult)
        {
            _scope = scope;
            _expressionStatementResult = expressionStatementResult;
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
            if (name == SourceObjectConsts.Scope)
            {
                return _expressionStatementResult;
            }

            if (name == SourceObjectConsts.ScopeWrappedScopeAttribute)
            {
                return new SourceValue(_scope);
            }

            return base.GetAttribute(name);
        }

        public override void SetAttribute(SourceValue name, SourceValue value)
        {
            if (name == SourceObjectConsts.Scope)
            {
                _expressionStatementResult = value;
                return;
            }

            throw new UnreachableException(nameof(SetAttribute));
        }
    }
}
