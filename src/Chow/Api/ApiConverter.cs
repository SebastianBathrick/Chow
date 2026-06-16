using Chow.SourceData;

namespace Chow
{
    static class ApiConverter
    {
        public static SourceValue Convert(IChowValue apiObj)
        {
            return ((ChowValue)apiObj).SourceValue;
        }
    }
}
