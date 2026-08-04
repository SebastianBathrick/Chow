using System;
using Chow.Interpreter.Exceptions;
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

        public static ChowObject ConvertToClass(SourceValue srcVal)
        {
            return new ChowObject(ref srcVal);
        }

        public static ChowObject[] ConvertToClass(SourceValue[] srcVals)
        {
            if (srcVals == null)
            {
                return Array.Empty<ChowObject>();
            }

            var classObjs = new ChowObject[srcVals.Length];

            for (var i = 0; i < srcVals.Length; i++)
            {
                classObjs[i] = ConvertToClass(srcVals[i]);
            }

            return classObjs;
        }

        public static SourceValue[] Convert(ChowObject[] apiVals)
        {
            if (apiVals == null)
            {
                return Array.Empty<SourceValue>();
            }

            var srcValValues = new SourceValue[apiVals.Length];

            for (var i = 0; i < apiVals.Length; i++)
            {
                srcValValues[i] = apiVals[i]?.SourceValue ?? SourceValue.None;
            }

            return srcValValues;
        }

        /// <summary>
        /// Converts native .NET call arguments for the virtual machine. Values that are already Chow
        /// objects are unwrapped rather than wrapped a second time, which is what keeps a public
        /// <see cref="ChowObject"/> from entering the VM as an opaque host object.
        /// </summary>
        public static SourceValue[] ConvertHostArguments(object[] hostArgs)
        {
            if (hostArgs == null)
            {
                return Array.Empty<SourceValue>();
            }

            var srcValValues = new SourceValue[hostArgs.Length];

            for (var i = 0; i < hostArgs.Length; i++)
            {
                srcValValues[i] = ConvertHostValue(hostArgs[i]);
            }

            return srcValValues;
        }

        /// <summary>
        /// Converts a single native .NET value for the virtual machine, used for both call arguments
        /// and the values host delegates return. A value that is already a Chow object is unwrapped
        /// rather than wrapped a second time, which is what keeps a public
        /// <see cref="ChowObject"/> from entering the VM as an opaque host object.
        /// </summary>
        public static SourceValue ConvertHostValue(object hostValue)
        {
            switch (hostValue)
            {
                // SourceValue's object constructor rejects null, and Chow spells absence as None.
                case null:
                    return SourceValue.None;
                case ChowObject chowObj:
                    return chowObj.SourceValue;
                case IChowObject apiObj:
                    return Convert(apiObj);
                default:
                    return new SourceValue(hostValue);
            }
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
