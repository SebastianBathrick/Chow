using Chow.SourceData;

namespace Chow
{
    /// <summary>
    /// Internal factory class to create <see cref="ChowObject"/> instances with a public API that
    /// exclusively references <see cref="IChowObject"/> to avoid internal dependencies on a library
    /// API class.
    /// </summary>
    static class ChowObjectFactory
    {
        public static IChowObject CreateObject(object val)
        {
            return val == null ? ChowObject.None : new ChowObject(new SourceValue(val));

        }
        
        public static IChowObject CreateDict()
        {
            var srcVal = SourceObjectFactory.CreateNewObject(DataType.Dict).ToSourceValue();
            return new ChowObject(ref srcVal);
        }

        public static IChowObject CreateList()
        {
            var srcVal = SourceObjectFactory.CreateNewObject(DataType.List).ToSourceValue();
            return new ChowObject(ref srcVal);
        }

        public static IChowObject CreateScope()
        {
            var srcVal = SourceObjectFactory.CreateNewObject(DataType.Scope).ToSourceValue();
            return new ChowObject(ref srcVal);
        }

        // Use interface IChowObject to avoid ChowObject dependencies
        internal static IChowObject Create(ref SourceValue srcVal)
        {
            return new ChowObject(ref srcVal);
        }

        internal static IChowObject Create(SourceValue srcVal)
        {
            return new ChowObject(ref srcVal);
        }
    }
}
