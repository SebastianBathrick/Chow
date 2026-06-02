using Chow.Exceptions;
namespace Chow.DataTypes
{
    static class IteratorFactory
    {
        public static IChowIterator GetIterator(ChowValue source)
        {
            switch (source.DataType)
            {
                case DataType.List:
                {
                    return new InternalListIterator(source.AsType<InternalList>());
                }
                case DataType.Str:
                {
                    return new InternalStrIterator(source.AsType<string>());
                }
                case DataType.Range:
                {
                    return source.AsType<InternalRange>().GetIterator();
                }
                case DataType.None:
                case DataType.Bool:
                case DataType.Object:
                case DataType.Int:
                case DataType.Float:
                case DataType.Dict:
                default:
                {
                    throw new TypeException($"'{TypeNameOf(source.DataType)}' object is not iterable");
                }
            }
        }

        static string TypeNameOf(DataType dataType)
        {
            switch (dataType)
            {
                case DataType.None:
                {
                    return "NoneType";
                }
                case DataType.Bool:
                {
                    return "bool";
                }
                case DataType.Int:
                {
                    return "int";
                }
                case DataType.Float:
                {
                    return "float";
                }
                case DataType.Object:
                case DataType.Str:
                case DataType.List:
                case DataType.Dict:
                case DataType.Range:
                default:
                {
                    return dataType.ToString().ToLowerInvariant();
                }
            }
        }
    }
}
