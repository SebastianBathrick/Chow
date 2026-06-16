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
            var srcObj = SourceObjectFactory.CreateNewObject(DataType.Dict);
            return new ChowValue(new SourceValue(srcObj));
        }
        
        public static IChowValue CreateList()
        {
            var srcObj = SourceObjectFactory.CreateNewObject(DataType.List);
            return new ChowValue(new SourceValue(srcObj));
        }

        // Use interface IChowValue to avoid ChowValue dependencies
        internal static IChowValue Create(SourceValue srcObj)
        {
            return new ChowValue(srcObj);

            
        }
    }
}
