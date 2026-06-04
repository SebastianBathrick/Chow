using System;
using Chow.DataTypes;
using Chow.Bytecode;
using Chow.Expressions;

namespace Chow.Exceptions
{
    class TypeException : Exception
    {   
        public TypeException(string message) : base(message)
        {
        }
        
        
        public TypeException(Tag lTag, Tag rTag, OperationCode binaryOp) : base(CreateOperandTypesMessage(lTag, rTag, binaryOp))
        {
        }

        static string CreateOperandTypesMessage(Tag lTag, Tag rTag, OperationCode binaryOp)
        {
            return "TypeError: unsupported operand type(s) for "
                + $"{OperatorStrings.EnumToString(binaryOp)}: "
                + $"'{DataTypeNames.GetTypeName(lTag)}' and "
                + $"'{DataTypeNames.GetTypeName(rTag)}'";
        }
    }
}
