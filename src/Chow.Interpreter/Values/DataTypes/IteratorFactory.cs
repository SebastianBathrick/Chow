using Chow.Interpreter.Exceptions;
namespace Chow.Interpreter.Values.DataTypes
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
                default:
                {
                    return dataType.ToString().ToLowerInvariant();
                }
            }
        }
    }
}
