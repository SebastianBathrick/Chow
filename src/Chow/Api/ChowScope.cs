using Chow.SourceData;

namespace Chow
{
    public class ChowScope : IChowValue
    {
        // Temporary solution
        readonly ChowValue _wrappedObject;

        public int Length => _wrappedObject.Length;

        public ChowValue ExpressionResult =>
            _wrappedObject.GetAttribute(SourceObjectConsts.ScopeExpressionResultAttribute);

        public ChowValue this[ChowValue key]
        {
            get => _wrappedObject[key];
            set => _wrappedObject[key] = value;
        }

        public ChowScope()
        {
            _wrappedObject = (ChowValue)ChowValueFactory.CreateScope();
        }

        internal ChowScope(ChowValue wrappedObject)
        {
            _wrappedObject = wrappedObject;
        }

        public static implicit operator ChowValue(ChowScope value)
        {
            return value._wrappedObject;
        }

        public static implicit operator ChowScope(ChowValue value)
        {
            return new ChowScope(value);
        }

        public override string ToString()
        {
            return _wrappedObject.ToString();
        }
    }
}
