using Chow.SourceData;

namespace Chow
{
    /// <summary>
    /// Internal factory class to create <see cref="ChowValue"/> instances with a public API that
    /// exclusively references <see cref="IChowValue"/> to avoid internal dependencies on a library
    /// API class.
    /// </summary>
    static class ChowValueFactory
    {
        public static IChowValue CreateDictionary()
        {
            var srcVal = SourceObjectFactory.CreateNewObject(DataType.Dict).ToSourceValue();
            return new ChowValue(ref srcVal);
        }

        public static IChowValue CreateList()
        {
            var srcVal = SourceObjectFactory.CreateNewObject(DataType.List).ToSourceValue();
            return new ChowValue(ref srcVal);
        }

        public static IChowValue CreateScope()
        {
            var srcVal = SourceObjectFactory.CreateNewObject(DataType.Scope).ToSourceValue();
            return new ChowValue(ref srcVal);
        }

        // Use interface IChowValue to avoid ChowValue dependencies
        internal static IChowValue Create(ref SourceValue srcVal)
        {
            return new ChowValue(ref srcVal);
        }

        internal static IChowValue Create(SourceValue srcVal)
        {
            return new ChowValue(ref srcVal);
        }
    }
}
