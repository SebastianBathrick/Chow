using Chow.Exceptions;
namespace Chow.DataTypes
{
    static class IteratorFactory
    {
        public static IIterator GetIterator(TaggedUnion source)
        {
            switch (source.Tag)
            {
                case Tag.List:
                {
                    return new ChowListIterator(source.AsType<ChowList>());
                }
                case Tag.Str:
                {
                    return new ChowStringIterator(source.AsType<string>());
                }
                case Tag.Range:
                {
                    return source.AsType<ChowRange>().GetIterator();
                }
                case Tag.None:
                case Tag.Bool:
                case Tag.Object:
                case Tag.Long:
                case Tag.Double:
                case Tag.Dict:
                default:
                {
                    throw new TypeException($"'{TypeNameOf(source.Tag)}' object is not iterable");
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
