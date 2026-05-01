using System;
using System.Collections.Generic;
using System.Text;

namespace Chow
{
    internal class Chunk
    {
        List<OperationCode> _code = new List<OperationCode>();
        List<ChowValue> _constants = new List<ChowValue>(); 

        public void  AddConstant(ChowValue newConstant)
        {
            _constants.Add(newConstant);
        }

        public void AppendCode(OperationCode operationCode)
        {
            _code.Add(operationCode);
        }
    }
}
