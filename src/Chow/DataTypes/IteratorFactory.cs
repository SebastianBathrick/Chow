using Chow.Exceptions;
namespace Chow.DataTypes
{
    static class IteratorFactory
    {
        public static IChowIterator GetIterator(TaggedUnion source)
        {
            switch (source.Type)
            {
                case Tag.List:
                {
                    return new InternalListIterator(source.AsType<InternalList>());
                }
                case Tag.Str:
                {
                    return new InternalStrIterator(source.AsType<string>());
                }
                case Tag.Range:
                {
                    return source.AsType<InternalRange>().GetIterator();
                }
                case Tag.None:
                case Tag.Bool:
                case Tag.Object:
                case Tag.Long:
                case Tag.Double:
                case Tag.Dict:
                default:
                {
                    throw new TypeException($"'{TypeNameOf(source.Type)}' object is not iterable");
                }
            }
        }

        static string TypeNameOf(Tag tag)
        {
            switch (tag)
            {
                case Tag.None:
                {
                    return "NoneType";
                }
                case Tag.Bool:
                {
                    return "bool";
                }
                case Tag.Long:
                {
                    return "int";
                }
                case Tag.Double:
                {
                    return "float";
                }
                case Tag.Object:
                case Tag.Str:
                case Tag.List:
                case Tag.Dict:
                case Tag.Range:
                default:
                {
                    return tag.ToString().ToLowerInvariant();
                }
            }
        }
    }
}
