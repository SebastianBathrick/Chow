using System.Collections.Generic;
using Chow.SourceData;

namespace Chow.Code
{
    class DataTypeNames
    {
        static readonly IReadOnlyDictionary<DataType, string> DataTypeNameMap = new Dictionary<DataType, string>
        {
            {
                DataType.None, "NoneType"
            },
            {
                DataType.Bool, "bool"
            },
            {
                DataType.Object, "object"
            },
            {
                DataType.Long, "int"
            },
            {
                DataType.Double, "float"
            },
            {
                DataType.Str, "str"
            },
            {
                DataType.List, "list"
            },
            {
                DataType.Dict, "dict"
            },
            {
                DataType.Range, "range"
            },
            {
                DataType.Function, "function"
            },
            {
                DataType.Slice, "slice"
            },
            {
                DataType.Scope, "scope"
            },
            {
                DataType.Class, "type"
            },
            {
                DataType.Instance, "object"
            }
        };

        public static string GetTypeName(DataType dataType)
        {
            // Falls back to the tag's own name so an unmapped type surfaces as a readable error
            // message rather than a KeyNotFoundException from inside the error path itself.
            return DataTypeNameMap.TryGetValue(dataType, out var typeName)
                ? typeName : dataType.ToString();
        }
    }
}
