using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Bytecode
{
    class Chunk
    {
        List<Operation> _operations = new List<Operation>();
        List<TaggedUnion> _constants = new List<TaggedUnion>(); 
        List<int> _operationLineNums = new List<int>();

        public int AddConstant(TaggedUnion newConstant)
        {
            _constants.Add(newConstant);
            return _constants.Count - 1; // Provide an index to use as an Operation operand
        }

        public void PushOperation(OperationCode operationType, int lineNumber, int operand = -1)
        {
            _operations.Add(new Operation(operationType, operand));
            _operationLineNums.Add(lineNumber);
        }
    }
}
