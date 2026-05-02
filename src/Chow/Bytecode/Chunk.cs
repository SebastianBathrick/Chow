using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Bytecode
{
    class Chunk
    {
        List<Operation> _code = new List<Operation>();
        List<ChowValue> _constants = new List<ChowValue>(); 

        public void  AddConstant(ChowValue newConstant)
        {
            _constants.Add(newConstant);
        }

        public void AddOperation(Operation operation)
        {
            _code.Add(operation);
        }
    }
}
