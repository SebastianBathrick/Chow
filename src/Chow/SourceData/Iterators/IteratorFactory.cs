using Chow.Code;
using Chow.Interpreter.Exceptions;

namespace Chow.SourceData
{
    static class IteratorFactory
    {
        /// <summary>Python's <c>iter()</c>: an iterator over <paramref name="source"/>.</summary>
        public static SourceIterator GetIterator(SourceValue source)
        {
            switch (source.DataType)
            {
                case DataType.List:
                case DataType.Range:
                case DataType.Dict:
                case DataType.DictKeys:
                case DataType.DictValues:
                case DataType.Iterator:
                case DataType.Set:
                case DataType.FrozenSet:
                    return source.ToISourceObject().GetIterator();
                case DataType.Str:
                    return new SourceStringIterator(source);
                default:
                    throw new DataTypeException(
                        $"'{DataTypeNames.GetTypeName(source)}' object is not iterable");
            }
        }

        /// <summary>Python's <c>reversed()</c>: an iterator over <paramref name="source"/> back to front.</summary>
        public static SourceIterator GetReversedIterator(SourceValue source)
        {
            switch (source.DataType)
            {
                case DataType.List:
                case DataType.Range:
                case DataType.Dict:
                case DataType.DictKeys:
                case DataType.DictValues:
                    return source.ToISourceObject().GetReversedIterator();
                case DataType.Str:
                    return new SourceReversedIterator(source);
                default:
                    throw new DataTypeException(
                        $"'{DataTypeNames.GetTypeName(source)}' object is not reversible");
            }
        }
    }
}
