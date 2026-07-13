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
            }
        };

        public static string GetTypeName(DataType dataType)
        {
            return DataTypeNameMap[dataType];
        }
    }
}
