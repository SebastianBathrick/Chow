using System;
using Chow.DataTypes;

namespace Chow.Exceptions
{
    class TypeException : Exception
    {   
        public TypeException()
        {
        }

        public TypeException(string message) : base(message)
        {
        }
        
        
        public TypeException(Tag lTag, Tag rTag, ExpressionOperator op) : base(CreateOperandTypesMessage(lTag, rTag, op))
        {
        }

        static string CreateOperandTypesMessage(Tag lTag, Tag rTag, ExpressionOperator op)
        {
            return "TypeError: unsupported operand type(s) for "
                + $"{OperatorStrings.GetOperatorString(op)}: "
                + $"'{DataTypeNames.GetTypeName(lTag)}' and "
                + $"'{DataTypeNames.GetTypeName(rTag)}'";
        }
    }
}
