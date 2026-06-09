using System;

namespace Chow.SourceData
{
    static class SourceObjectFactory
    {
        public static ISourceObject GetSourceObject(DataType srcObjType)
        {
            switch (srcObjType)
            {
                case DataType.List:
                    return new SourceList();
                case DataType.Dict:
                    return new SourceDictionary();
                default:
                    // Range/function/slice carry constructor state and are created directly.
                    throw new ArgumentOutOfRangeException(nameof(srcObjType), srcObjType, null);
            }
        }
    }
}
