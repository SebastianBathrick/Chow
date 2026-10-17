using Chow.Code;
using Chow.Interpreter.Exceptions;

namespace Chow.SourceData
{
    static class IteratorFactory
    {
        public static IIterator GetIterator(SourceValue source)
        {
            switch (source.DataType)
            {
                case DataType.List:
                case DataType.Range:
                case DataType.Dict:
                case DataType.DictKeys:
                case DataType.DictValues:
                    return source.ToISourceObject().GetIterator();
                case DataType.Str:
                    return new SourceStringIterator(source);
                default:
                    throw new DataTypeException(
                        $"'{DataTypeNames.GetTypeName(source.DataType)}' object is not iterable");
            }
        }
    }
}
