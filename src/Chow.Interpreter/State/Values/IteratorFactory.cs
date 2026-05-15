using Chow.Interpreter.Exceptions;

namespace Chow.Interpreter.State.Values
{
    static class IteratorFactory
    {
        public static IChowIterator GetIterator(TaggedUnion source)
        {
            switch (source.Tag)
            {
                case Tag.List:
                {
                    return new InternalListIterator(source.ListValue);
                }
                case Tag.Str:
                {
                    return new InternalStrIterator(source.StringValue);
                }
                case Tag.Range:
                {
                    return source.RangeValue.GetIterator();
                }
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
                case Tag.Boolean:
                {
                    return "bool";
                }
                case Tag.Int:
                {
                    return "int";
                }
                case Tag.Float:
                {
                    return "float";
                }
                default:
                {
                    return tag.ToString().ToLowerInvariant();
                }
            }
        }
    }
}
