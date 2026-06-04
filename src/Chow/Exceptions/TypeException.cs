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
        
        
        public TypeException(Tag lTag, Tag rTag, ExpressionOperator @operator) : base(CreateOperandTypesMessage(lTag, rTag, @operator))
        {
        }

        static string CreateOperandTypesMessage(Tag lTag, Tag rTag)
        {
            return $"TypeError: unsupported operand type(s) for {op}: '{lTag}' and '{rTag}'";
        }
    }
}
