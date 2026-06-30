using Chow.SourceData;

namespace Chow
{
    static class ApiConverter
    {
        public static IChowObject Convert(SourceValue srcVal)
        {
            return new ChowObject(ref srcVal);
        }
        
        public static IChowObject Convert(ref SourceValue srcVal)
        {
            return new ChowObject(ref srcVal);
        }

        public static SourceValue[] Convert(IChowObject[] apiVals)
        {
            var srcValValues = new SourceValue[apiVals.Length];
            
            for (var i = 0; i < apiVals.Length; i++)
            {
                srcValValues[i] = Convert(apiVals[i]);
            }

            return srcValValues;
        }

        public static IChowObject[] ConvertToInterface(ChowObject[] classObjs)
        {
            if (classObjs == null)
            {
                return null;
            }
            
            var interfaceVals = new IChowObject[classObjs.Length];

            for (var i = 0; i < classObjs.Length; i++)
            {
                interfaceVals[i] = classObjs[i];
            }
            
            return interfaceVals;
        }

        public static SourceValue Convert(IChowObject apiObj)
        {
            if (apiObj is ChowObject chowObj)
            {
                return chowObj.SourceValue;
            }

            return GetWrappedChowObject(apiObj).SourceValue;
        }

        public static ChowObject GetWrappedChowObject(IChowObject obj)
        {
            switch (obj)
            {
                case ChowDict dict:
                    return dict.WrappedObject;
                case ChowList list:
                    return list.WrappedObject;
                case ChowScope scope:
                    return scope.WrappedObject;
                case ChowString str:
                    return str.WrappedObject;
                default:
                    throw new UnreachableException(nameof(GetWrappedChowObject));
            }
        }
    }
}
