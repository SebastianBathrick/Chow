using Chow.SourceData;

namespace Chow
{
    static class ApiConverter
    {
        public static IChowValue Convert(SourceValue srcVal)
        {
            return new ChowValue(ref srcVal);
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
