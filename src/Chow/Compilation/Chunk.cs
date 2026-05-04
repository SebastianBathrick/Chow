using Chow.Interpreter.Values;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chow.Interpreter.Jit
{
    class Chunk
    {
        List<Operation> _operations = new List<Operation>();
        List<TaggedUnion> _consts = new List<TaggedUnion>();
        List<int> _operationLineNums = new List<int>();

        public int Count => _operations.Count;

        public Operation this[int index] => _operations[index];

        public void PushOperation(OperationCode operationType, int lineNumber, int operand = -1)
        {
            _operations.Add(new Operation(operationType, operand));
            _operationLineNums.Add(lineNumber);
        }

        #region Constant Methods

        public TaggedUnion ReadConstant(int operand) => _consts[operand];

        /// <summary>
        /// Stores a constant value and returns an integer for use as an operand assigned to <see cref="Operation"/> instance(s).
        /// </summary>
        /// <param name="newConst">TaggedUnion containing a constant primitive value.</param>
        /// <returns>Integer representing the operand used to read the constant at runtime.</returns>
        /// <remarks>If an existing constant has the same value as <paramref name="newConst"/> then the operand for 
        /// that existing constant will be returned. Otherwise, the new constant is stored and a new operand is returned</remarks>
        public int RegisterConstant(TaggedUnion newConst)
        {
            int constIndex = FindConstantIndex(newConst);

            if (constIndex < 0)
            {
                constIndex = _consts.Count;
                _consts.Add(newConst);
            }

            return constIndex;
        }

        // The constant list's index is only to be refered to as "operand" in the public API to hide interal functionality
        int FindConstantIndex(TaggedUnion constant) => _consts.IndexOf(constant);

        #endregion

        #region Variable Name Methods

        // TEMPORARY LOGIC: This public API is intended to abstract how variable identifiers are stored and accessed.
        // This public API will remain the same, but their function logic will change later in developement. Currently
        // variables are internally stored the exact same way as constants, and internally retrieved the exact same way
        // as constants due to time constraints. However, all variable-name related logic will be accessed by the client
        // via dedicated variable methods, so when variable-names are stored differently in Chunk, no client code will 
        // need to be changed.

        // NOTE: Making the ReadConstant call does return a new struct, and that is slower, but it is temporary and I want
        // them to work identically as constants for the time being. Less code to manage.
        public string ReadVariableName(int operand) => ReadConstant(operand).StringValue;

        // This is one piece of functionality that constant will never have publically (still going to change internally for variables)
        public int FindVariableName(string varName) => FindConstantIndex(new TaggedUnion(varName));

        /// <summary>
        /// Used to register a variable name compile-time and return an operand for use in <see cref="Operation"/> instance(s) 
        /// that declare or reference that variable.
        /// </summary>
        /// <param name="varName">Variable name to register.</param>
        /// <returns>If an existing constant has the same value as <paramref name="varName"/> then the operand for 
        /// that existing constant will be returned. Otherwise, the new variable name is stored and a new operand is returned.</returns>
        /// <remarks>This is ONLY for storing variable names COMPILE-TIME. NOT for storing variable names runtime, AND NOT NEVER
        /// for storing variable values. </remarks>
        public int RegisterVariableName(string varName) => RegisterConstant(new TaggedUnion(varName));

        #endregion

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine("Constants:");
            for (int i = 0; i < _consts.Count; i++)
            {
                TaggedUnion constant = _consts[i];

                sb.Append("  ");
                sb.Append(i);
                sb.Append(": ");

                if (constant.IsInteger)
                {
                    sb.Append("Int=");
                    sb.Append(constant.IntegerValue);
                }
                else if (constant.IsFloat)
                {
                    sb.Append("Float=");
                    sb.Append(constant.FloatValue);
                }

                sb.AppendLine();
            }

            sb.AppendLine("Operations:");
            for (int i = 0; i < _operations.Count; i++)
            {
                Operation op = _operations[i];

                sb.Append("  ");
                sb.Append(i);
                sb.Append(": ");
                sb.Append(op.Code);

                if (op.Operand != -1)
                {
                    TaggedUnion constant = _consts[op.Operand];

                    sb.Append(' ');
                    sb.Append(op.Operand);
                    sb.Append(" (");

                    if (constant.IsInteger)
                    {
                        sb.Append("Int=");
                        sb.Append(constant.IntegerValue);
                    }
                    else if (constant.IsFloat)
                    {
                        sb.Append("Float=");
                        sb.Append(constant.FloatValue);
                    }

                    sb.Append(')');
                }

                if (i < _operations.Count - 1)
                {
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }
    }
}
