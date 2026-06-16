using System;

namespace Chow.SourceData
{
    static class SourceObjectFactory
    {
        public static ISourceObject CreateNewObject(DataType srcObjType)
        {
            switch (srcObjType)
            {
                case DataType.List:
                    return new SourceList();
                case DataType.Dict:
                    return new SourceDictionary();
                case DataType.Scope:
                    return new SourceScope(new Scope(), SourceValue.None);
                default:
                    // Range/function/slice carry constructor state and are created directly.
                    throw new ArgumentOutOfRangeException(nameof(srcObjType), srcObjType, null);
            }
        }
    }
}
