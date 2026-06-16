namespace Chow.SourceData
{
    sealed class SourceScope : SourceObject
    {
        readonly Scope _scope;
        SourceValue _expressionStatementResult;

        public override DataType Type => DataType.Scope;

        public SourceScope(Scope scope, SourceValue expressionStatementResult)
        {
            _scope = scope;
            _expressionStatementResult = expressionStatementResult;
        }

        public override SourceValue GetAttribute(SourceValue name)
        {
            if (name == SourceObjectConsts.ExpressionResultAttribute)
            {
                return _expressionStatementResult;
            }

            return _scope.GetVariableValue(name.ToString());
        }

        public override void SetAttribute(SourceValue name, SourceValue value)
        {
            if (name == SourceObjectConsts.ExpressionResultAttribute)
            {
                _expressionStatementResult = value;
                return;
            }

            _scope.AssignVariableValue(name.ToString(), ref value);
        }
    }
}
