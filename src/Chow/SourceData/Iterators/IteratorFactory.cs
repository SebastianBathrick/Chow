using Chow.VM;

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
                    return source.ToISourceObject().GetIterator();
                case DataType.Str:
                    return new SourceStringIterator(source);
                case DataType.None:
                case DataType.Bool:
                case DataType.Object:
                case DataType.Long:
                case DataType.Double:
                case DataType.Dict:
                default:
                    throw new DataTypeException($"'{TypeNameOf(source.DataType)}' object is not iterable");
            }
        }

        static string TypeNameOf(DataType dataType)
        {
            switch (dataType)
            {
                case DataType.None:
                    return "NoneType";
                case DataType.Bool:
                    return "bool";
                case DataType.Long:
                    return "int";
                case DataType.Double:
                    return "float";
                case DataType.Object:
                case DataType.Str:
                case DataType.List:
                case DataType.Dict:
                case DataType.Range:
                default:
                    return dataType.ToString().ToLowerInvariant();
            }
        }
    }
}
