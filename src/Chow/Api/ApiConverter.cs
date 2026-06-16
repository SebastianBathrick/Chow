using Chow.SourceData;

namespace Chow
{
    static class ApiConverter
    {
        public static ChowValue[] Convert(SourceValue[] values)
        {
            var chowValues = new ChowValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                chowValues[i] = new ChowValue(values[i]);
            }

            return chowValues;
        }

        public static SourceValue[] Convert(ChowValue[] values)
        {
            var srcValValues = new SourceValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                srcValValues[i] = values[i].SourceValue;
            }
            return srcValValues;
        }
        
        public static SourceValue Convert(IChowValue apiObj)
        {
            return ((ChowValue)apiObj).SourceValue;
        }
    }
}
