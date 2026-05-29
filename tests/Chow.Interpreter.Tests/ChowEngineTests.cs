namespace Chow.Interpreter.Tests
{
    [TestFixture]
    public class ChowEngineTests
    {
        #region Execute_ExpressionStatement_ReturnsCorrectValue Logic & Declarations
        
        //=========================================================================================
        // Constants
        //=========================================================================================
        
        // --- Integer arithmetic & operator precedence ---
        const string CHOW_ADD_MULTIPLY_PRECEDENCE = "2 + 3 * 4";
        const long CHOW_ADD_MULTIPLY_PRECEDENCE_RESULT = 14L;

        const string CHOW_ADD_MULTIPLY_PARENTHESIS_PRECEDENCE = "(2 + 3) * 4";
        const long CHOW_ADD_MULTIPLY_PARENTHESIS_PRECEDENCE_RESULT = 20L;

        const string CHOW_SUBTRACTION = "20 - 7";
        const long CHOW_SUBTRACTION_RESULT = 13L;

        const string CHOW_FLOOR_DIVISION = "10 // 4";
        const long CHOW_FLOOR_DIVISION_RESULT = 2L;

        const string CHOW_MODULO = "10 % 4";
        const long CHOW_MODULO_RESULT = 2L;

        const string CHOW_EXPONENT_PRECEDENCE = "2 ** 3 ** 2";
        const long CHOW_EXPONENT_PRECEDENCE_RESULT = 512L;

        // --- Float & mixed-numeric arithmetic ---
        const string CHOW_FLOAT_DIVISION = "10 / 4";
        const double CHOW_FLOAT_DIVISION_RESULT = 2.5;

        const string CHOW_MIXED_INT_FLOAT_SUM = "3 + 2.5";
        const double CHOW_MIXED_INT_FLOAT_SUM_RESULT = 5.5;

        const string CHOW_NEGATIVE_EXPONENT = "2 ** -1";
        const double CHOW_NEGATIVE_EXPONENT_RESULT = 0.5;

        // --- Unary negation ---
        const string CHOW_UNARY_NEGATION = "3 * -(4 + 1)";
        const long CHOW_UNARY_NEGATION_RESULT = -15L;

        const string CHOW_LEADING_UNARY_NEGATION = "-(4 + 1)";
        const long CHOW_LEADING_UNARY_NEGATION_RESULT = -5L;

        // --- String operations ---
        const string CHOW_STRING_CONCATENATION = "\"foo\" + \"bar\"";
        const string CHOW_STRING_CONCATENATION_RESULT = "foobar";

        const string CHOW_STRING_REPETITION = "\"ab\" * 3";
        const string CHOW_STRING_REPETITION_RESULT = "ababab";

        // --- Comparisons ---
        const string CHOW_COMPARISON_GREATER = "5 > 3";
        const bool CHOW_COMPARISON_GREATER_RESULT = true;

        const string CHOW_CHAINED_COMPARISON = "1 < 2 < 3";
        const bool CHOW_CHAINED_COMPARISON_RESULT = true;

        const string CHOW_EQUALITY_INT_FLOAT = "1 == 1.0";
        const bool CHOW_EQUALITY_INT_FLOAT_RESULT = true;

        // --- Boolean logic ---
        const string CHOW_BOOLEAN_AND = "True and False";
        const bool CHOW_BOOLEAN_AND_RESULT = false;

        const string CHOW_BOOLEAN_OR_PRECEDENCE = "False or 2 > 1";
        const bool CHOW_BOOLEAN_OR_PRECEDENCE_RESULT = true;

        const string CHOW_LOGICAL_NOT = "not 0";
        const bool CHOW_LOGICAL_NOT_RESULT = true;

        //=========================================================================================
        // Methods
        //=========================================================================================
        
        [TestCaseSource(nameof(AddExpressionStatementCases))]
        public ChowValue Execute_ExpressionStatement_ReturnsCorrectValue(string sourceCode)
        {
            ChowEngine.Reset();

            return ChowEngine.Execute(sourceCode);
        }
        
        static IEnumerable<TestCaseData> AddExpressionStatementCases()
        {
            // --- Integer arithmetic & operator precedence ---
            yield return new TestCaseData(CHOW_ADD_MULTIPLY_PRECEDENCE)
                .Returns(new ChowValue(CHOW_ADD_MULTIPLY_PRECEDENCE_RESULT));
            yield return new TestCaseData(CHOW_ADD_MULTIPLY_PARENTHESIS_PRECEDENCE)
                .Returns(new ChowValue(CHOW_ADD_MULTIPLY_PARENTHESIS_PRECEDENCE_RESULT));
            yield return new TestCaseData(CHOW_SUBTRACTION)
                .Returns(new ChowValue(CHOW_SUBTRACTION_RESULT));
            yield return new TestCaseData(CHOW_FLOOR_DIVISION)
                .Returns(new ChowValue(CHOW_FLOOR_DIVISION_RESULT));
            yield return new TestCaseData(CHOW_MODULO)
                .Returns(new ChowValue(CHOW_MODULO_RESULT));
            yield return new TestCaseData(CHOW_EXPONENT_PRECEDENCE)
                .Returns(new ChowValue(CHOW_EXPONENT_PRECEDENCE_RESULT));

            // --- Float & mixed-numeric arithmetic ---
            yield return new TestCaseData(CHOW_FLOAT_DIVISION)
                .Returns(new ChowValue(CHOW_FLOAT_DIVISION_RESULT));
            yield return new TestCaseData(CHOW_MIXED_INT_FLOAT_SUM)
                .Returns(new ChowValue(CHOW_MIXED_INT_FLOAT_SUM_RESULT));
            yield return new TestCaseData(CHOW_NEGATIVE_EXPONENT)
                .Returns(new ChowValue(CHOW_NEGATIVE_EXPONENT_RESULT));

            // --- Unary negation ---
            yield return new TestCaseData(CHOW_UNARY_NEGATION)
                .Returns(new ChowValue(CHOW_UNARY_NEGATION_RESULT));
            yield return new TestCaseData(CHOW_LEADING_UNARY_NEGATION)
                .Returns(new ChowValue(CHOW_LEADING_UNARY_NEGATION_RESULT));

            // --- String operations ---
            yield return new TestCaseData(CHOW_STRING_CONCATENATION)
                .Returns(new ChowValue(CHOW_STRING_CONCATENATION_RESULT));
            yield return new TestCaseData(CHOW_STRING_REPETITION)
                .Returns(new ChowValue(CHOW_STRING_REPETITION_RESULT));

            // --- Comparisons ---
            yield return new TestCaseData(CHOW_COMPARISON_GREATER)
                .Returns(new ChowValue(CHOW_COMPARISON_GREATER_RESULT));
            yield return new TestCaseData(CHOW_CHAINED_COMPARISON)
                .Returns(new ChowValue(CHOW_CHAINED_COMPARISON_RESULT));
            yield return new TestCaseData(CHOW_EQUALITY_INT_FLOAT)
                .Returns(new ChowValue(CHOW_EQUALITY_INT_FLOAT_RESULT));

            // --- Boolean logic ---
            yield return new TestCaseData(CHOW_BOOLEAN_AND)
                .Returns(new ChowValue(CHOW_BOOLEAN_AND_RESULT));
            yield return new TestCaseData(CHOW_BOOLEAN_OR_PRECEDENCE)
                .Returns(new ChowValue(CHOW_BOOLEAN_OR_PRECEDENCE_RESULT));
            yield return new TestCaseData(CHOW_LOGICAL_NOT)
                .Returns(new ChowValue(CHOW_LOGICAL_NOT_RESULT));
        }
                
        #endregion
    }
}
