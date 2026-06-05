using System;
using System.Collections.Generic;
using System.Text;


namespace Chow.Expressions
{
    interface ITagConversionMap
    {
        IReadOnlyDictionary<(Tag, Tag), Tag> ConversionRules { get; }
    }
}
