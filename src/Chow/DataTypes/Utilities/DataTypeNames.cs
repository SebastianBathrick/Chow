using System.Collections.Generic;

namespace Chow.DataTypes
{
    public class DataTypeNames
    {
        static readonly IReadOnlyDictionary<Tag, string> DataTypeNameMap = new Dictionary<Tag, string>()
        {
            { Tag.None, "None" },
            { Tag.Bool, "bool" },
            { Tag.Long, "int" },
            { Tag.Double, "float" },
            { Tag.Str, "str" },
            { Tag.List, "list" },
            { Tag.Dict, "dict" },
            { Tag.Range, "range" }
        };

        public static string GetTypeName(Tag tag)
        {
            return DataTypeNameMap[tag];
        }
    }
}
