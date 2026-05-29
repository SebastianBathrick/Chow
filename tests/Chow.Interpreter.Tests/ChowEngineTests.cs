namespace Chow.Interpreter.Tests
{
    [TestFixture]
    public class ChowEngineTests
    {
        #region Helpers

        [SetUp]
        public void SetupChowEngine()
        {
            ChowEngine.Reset();
        }

        #endregion
        
        #region 1.) Execute and Validate Expression Statements

        #region 1.1) Constants

        #region Integer Arithmetic & Operator Precedence

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

        const string CHOW_NESTED_PARENS = "((1 + 2) * (3 + 4))";
        const long CHOW_NESTED_PARENS_RESULT = 21L;

        const string CHOW_EXPONENT_ADD_MULTIPLY = "2 ** 3 + 4 * 5";
        const long CHOW_EXPONENT_ADD_MULTIPLY_RESULT = 28L;

        const string CHOW_MULTIPLY_EXPONENT_PRECEDENCE = "(2 + 3) * (4 - 1) ** 2";
        const long CHOW_MULTIPLY_EXPONENT_PRECEDENCE_RESULT = 45L;

        const string CHOW_SUBTRACTION_LEFT_ASSOCIATIVE = "10 - 3 - 2";
        const long CHOW_SUBTRACTION_LEFT_ASSOCIATIVE_RESULT = 5L;

        #endregion

        #region Unary Negation 

        const string CHOW_UNARY_NEGATION = "3 * -(4 + 1)";
        const long CHOW_UNARY_NEGATION_RESULT = -15L;

        // --- Python-parity gaps: LOGIC ERROR (op is implemented, but a code path wrongly rejects it) ---
        // Unary negation exists (Parser.ParseFactor), but Parser.IsPrimaryToken() omits SymbolMinus,
        // so an expression statement cannot START with '-'. Throws ParserEx "Expected statement".
        const string CHOW_LEADING_UNARY_NEGATION = "-(4 + 1)";
        const long CHOW_LEADING_UNARY_NEGATION_RESULT = -5L;

        // Same root cause as above: the leading '-' (not the nesting depth) is what the parser rejects.
        const string CHOW_LEADING_UNARY_NESTED = "-(-(5))";
        const long CHOW_LEADING_UNARY_NESTED_RESULT = 5L;

        const string CHOW_LOGICAL_NOT = "not 0";
        const bool CHOW_LOGICAL_NOT_RESULT = true;

        const string CHOW_DOUBLE_NOT = "not not True";
        const bool CHOW_DOUBLE_NOT_RESULT = true;

        const string CHOW_NOT_COMPARISON_PRECEDENCE = "not 1 == 1";
        const bool CHOW_NOT_COMPARISON_PRECEDENCE_RESULT = false;

        #endregion

        #region Negative Integer Arithmetic

        const string CHOW_NEGATIVE_MODULO = "(-7) % 3";
        const long CHOW_NEGATIVE_MODULO_RESULT = 2L;

        const string CHOW_NEGATIVE_FLOOR_DIVISION = "(-7) // 2";
        const long CHOW_NEGATIVE_FLOOR_DIVISION_RESULT = -4L;

        const string CHOW_SUBTRACT_NEGATIVE = "3 - -2";
        const long CHOW_SUBTRACT_NEGATIVE_RESULT = 5L;

        #endregion

        #region Float & Mixed-Numeric (Integer & Float) Arithmetic

        const string CHOW_FLOAT_DIVISION = "10 / 4";
        const double CHOW_FLOAT_DIVISION_RESULT = 2.5;

        const string CHOW_MIXED_INT_FLOAT_SUM = "3 + 2.5";
        const double CHOW_MIXED_INT_FLOAT_SUM_RESULT = 5.5;

        const string CHOW_NEGATIVE_EXPONENT = "2 ** -1";
        const double CHOW_NEGATIVE_EXPONENT_RESULT = 0.5;

        const string CHOW_INT_TRUE_DIVISION = "9 / 3";
        const double CHOW_INT_TRUE_DIVISION_RESULT = 3.0;

        // Written as an expression so the C# compile-time IEEE-754 fold matches the engine's runtime double addition.
        const string CHOW_FLOAT_PRECISION = "0.1 + 0.2";
        const double CHOW_FLOAT_PRECISION_RESULT = 0.1 + 0.2;

        const string CHOW_FLOAT_FLOOR_DIVISION = "7.0 // 2";
        const double CHOW_FLOAT_FLOOR_DIVISION_RESULT = 3.0;

        const string CHOW_FLOAT_MODULO = "5.5 % 2";
        const double CHOW_FLOAT_MODULO_RESULT = 1.5;

        const string CHOW_INT_FLOAT_PRODUCT = "2 * 3.0";
        const double CHOW_INT_FLOAT_PRODUCT_RESULT = 6.0;

        #endregion

        #region Comparison Operators

        const string CHOW_COMPARISON_GREATER = "5 > 3";
        const bool CHOW_COMPARISON_GREATER_RESULT = true;

        const string CHOW_CHAINED_COMPARISON = "1 < 2 < 3";
        const bool CHOW_CHAINED_COMPARISON_RESULT = true;

        const string CHOW_EQUALITY_INT_FLOAT = "1 == 1.0";
        const bool CHOW_EQUALITY_INT_FLOAT_RESULT = true;

        const string CHOW_CHAINED_COMPARISON_GREATER = "5 > 3 > 1";
        const bool CHOW_CHAINED_COMPARISON_GREATER_RESULT = true;

        const string CHOW_CHAINED_COMPARISON_FALSE = "1 < 2 < 2";
        const bool CHOW_CHAINED_COMPARISON_FALSE_RESULT = false;

        const string CHOW_CHAINED_COMPARISON_MIXED_EQUALITY = "1 == 1 < 2";
        const bool CHOW_CHAINED_COMPARISON_MIXED_EQUALITY_RESULT = true;

        const string CHOW_EQUALITY_BOOL_INT = "True == 1";
        const bool CHOW_EQUALITY_BOOL_INT_RESULT = true;

        const string CHOW_EQUALITY_INT_STRING = "1 == \"1\"";
        const bool CHOW_EQUALITY_INT_STRING_RESULT = false;

        const string CHOW_INEQUALITY_INT_FLOAT = "1 != 1.0";
        const bool CHOW_INEQUALITY_INT_FLOAT_RESULT = false;

        const string CHOW_STRING_COMPARISON = "\"apple\" < \"banana\"";
        const bool CHOW_STRING_COMPARISON_RESULT = true;

        #endregion

        #region Logic Operators

        const string CHOW_BOOLEAN_AND = "True and False";
        const bool CHOW_BOOLEAN_AND_RESULT = false;

        const string CHOW_BOOLEAN_OR_PRECEDENCE = "False or 2 > 1";
        const bool CHOW_BOOLEAN_OR_PRECEDENCE_RESULT = true;

        // `and`/`or` yield the surviving operand value (Python semantics), not a coerced bool.
        const string CHOW_AND_RETURNS_OPERAND = "True and 5";
        const long CHOW_AND_RETURNS_OPERAND_RESULT = 5L;

        const string CHOW_OR_RETURNS_OPERAND = "0 or 42";
        const long CHOW_OR_RETURNS_OPERAND_RESULT = 42L;

        const string CHOW_OR_RETURNS_STRING = "False or \"fallback\"";
        const string CHOW_OR_RETURNS_STRING_RESULT = "fallback";

        const string CHOW_AND_RETURNS_FALSY = "1 and 0";
        const long CHOW_AND_RETURNS_FALSY_RESULT = 0L;

        #endregion

        #region Parenthesis

        const string CHOW_REDUNDANT_PARENS = "(((42)))";
        const long CHOW_REDUNDANT_PARENS_RESULT = 42L;

        const string CHOW_SHALLOW_NESTED_PRODUCT = "(2 * (3 + 4))";
        const long CHOW_SHALLOW_NESTED_PRODUCT_RESULT = 14L;

        const string CHOW_SPACED_PARENS = "( 1 + 2 ) * 3";
        const long CHOW_SPACED_PARENS_RESULT = 9L;

        const string CHOW_REDUNDANT_PARENS_LITERAL = "((((5))))";
        const long CHOW_REDUNDANT_PARENS_LITERAL_RESULT = 5L;

        // --- Nesting: adjacent parenthesized groups ---
        const string CHOW_ADJACENT_GROUPS_PRODUCT = "(1 + 2) * (3 + 4) * (5 + 6)";
        const long CHOW_ADJACENT_GROUPS_PRODUCT_RESULT = 231L;

        const string CHOW_ADJACENT_GROUPS_SUBTRACTION = "(1 + 2) - (3 - 4) - (5 - 6)";
        const long CHOW_ADJACENT_GROUPS_SUBTRACTION_RESULT = 5L;

        const string CHOW_PARENTHESIZED_EXPONENT = "(2 + 3) * (4 - 1) ** (1 + 1)";
        const long CHOW_PARENTHESIZED_EXPONENT_RESULT = 45L;

        const string CHOW_NESTED_COMPARISON = "((1 + 2) * 3) > (4 + (5 * 0))";
        const bool CHOW_NESTED_COMPARISON_RESULT = true;

        // --- Nesting: adjacent/stacked unary operators (messy spacing) ---
        const string CHOW_ADJACENT_MINUS = "2--3";
        const long CHOW_ADJACENT_MINUS_RESULT = 5L;

        const string CHOW_STACKED_MINUS = "2 - - - 3";
        const long CHOW_STACKED_MINUS_RESULT = -1L;

        const string CHOW_NESTED_UNARY_PRODUCT = "3 * -(-(-3))";
        const long CHOW_NESTED_UNARY_PRODUCT_RESULT = -9L;

        const string CHOW_TRIPLE_NOT = "not not not True";
        const bool CHOW_TRIPLE_NOT_RESULT = false;

        // --- Nesting: deep arithmetic (left/right associative) ---
        const string CHOW_RIGHT_NESTED_SUM = "(1 + (2 + (3 + (4 + (5 + 6)))))";
        const long CHOW_RIGHT_NESTED_SUM_RESULT = 21L;

        const string CHOW_LEFT_NESTED_SUM = "((((1 + 2) + 3) + 4) + 5)";
        const long CHOW_LEFT_NESTED_SUM_RESULT = 15L;

        const string CHOW_DEEP_MIXED_PRECEDENCE = "(((2 * 3) + (4 * 5)) - ((6 / 2) + 1))";
        const double CHOW_DEEP_MIXED_PRECEDENCE_RESULT = 22.0;

        // --- Nesting: extremely deep parentheses (50 levels) ---
        // Exactly 50 '(' then 5 then 50 ')' (five 10-char chunks each side; const-folded at compile time).
        const string CHOW_DEEP_PARENS =
            "((((((((((" + "((((((((((" + "((((((((((" + "((((((((((" + "((((((((((" +
            "5" +
            "))))))))))" + "))))))))))" + "))))))))))" + "))))))))))" + "))))))))))";

        const long CHOW_DEEP_PARENS_RESULT = 5L;

        // --- Nesting: chained subscripts & nested containers ---
        const string CHOW_NESTED_LIST_SUBSCRIPT = "[[1, 2], [3, 4]][1][0]";
        const long CHOW_NESTED_LIST_SUBSCRIPT_RESULT = 3L;

        const string CHOW_NESTED_DICT_SUBSCRIPT = "{\"a\": {\"b\": 5}}[\"a\"][\"b\"]";
        const long CHOW_NESTED_DICT_SUBSCRIPT_RESULT = 5L;

        const string CHOW_SUBSCRIPT_EXPRESSION_INDEX = "[1, 2, 3][(1 + 1)]";
        const long CHOW_SUBSCRIPT_EXPRESSION_INDEX_RESULT = 3L;

        // --- Nesting: inside f-strings ---
        const string CHOW_FSTRING_NESTED_ARITHMETIC = "f\"{(1 + 2) * 3}\"";
        const string CHOW_FSTRING_NESTED_ARITHMETIC_RESULT = "9";

        const string CHOW_FSTRING_NESTED_COMPARISON = "f\"{(2 + 3) > (1 + 1)}\"";
        const string CHOW_FSTRING_NESTED_COMPARISON_RESULT = "True";

        // --- Nesting: deep boolean & comparison ---
        const string CHOW_DEEP_BOOLEAN = "(True and (False or (True and True)))";
        const bool CHOW_DEEP_BOOLEAN_RESULT = true;

        const string CHOW_NESTED_COMPARISON_BOOLEAN = "((1 < 2) and (2 < 3)) or (4 < 0)";
        const bool CHOW_NESTED_COMPARISON_BOOLEAN_RESULT = true;

        // --- Nesting: operator associativity (messy) ---
        const string CHOW_EXPONENT_RIGHT_ASSOCIATIVE = "2 ** 2 ** 3";
        const long CHOW_EXPONENT_RIGHT_ASSOCIATIVE_RESULT = 256L;

        const string CHOW_NEGATIVE_BASE_EVEN_EXPONENT = "(-2) ** 2";
        const long CHOW_NEGATIVE_BASE_EVEN_EXPONENT_RESULT = 4L;

        const string CHOW_NEGATIVE_BASE_ODD_EXPONENT = "(-2) ** 3";
        const long CHOW_NEGATIVE_BASE_ODD_EXPONENT_RESULT = -8L;

        const string CHOW_MODULO_LEFT_ASSOCIATIVE = "10 % 3 % 2";
        const long CHOW_MODULO_LEFT_ASSOCIATIVE_RESULT = 1L;

        const string CHOW_FLOOR_DIVISION_LEFT_ASSOCIATIVE = "100 // 7 // 2";
        const long CHOW_FLOOR_DIVISION_LEFT_ASSOCIATIVE_RESULT = 7L;

        #endregion

        #region String Operations

        const string CHOW_STRING_CONCATENATION = "\"foo\" + \"bar\"";
        const string CHOW_STRING_CONCATENATION_RESULT = "foobar";

        const string CHOW_STRING_REPETITION = "\"ab\" * 3";
        const string CHOW_STRING_REPETITION_RESULT = "ababab";

        const string CHOW_STRING_CONCATENATION_CHAIN = "\"ab\" + \"cd\" + \"ef\"";
        const string CHOW_STRING_CONCATENATION_CHAIN_RESULT = "abcdef";

        const string CHOW_STRING_REPETITION_SYMBOL = "\"=\" * 5";
        const string CHOW_STRING_REPETITION_SYMBOL_RESULT = "=====";

        const string CHOW_INT_STRING_REPETITION = "3 * \"ab\"";
        const string CHOW_INT_STRING_REPETITION_RESULT = "ababab";

        #endregion

        #region F-Strings

        const string CHOW_FSTRING_EXPRESSION = "f\"{1 + 1}\"";
        const string CHOW_FSTRING_EXPRESSION_RESULT = "2";

        const string CHOW_FSTRING_WITH_TEXT = "f\"sum={2 + 3}\"";
        const string CHOW_FSTRING_WITH_TEXT_RESULT = "sum=5";

        const string CHOW_FSTRING_BOOL = "f\"{2 > 1}\"";
        const string CHOW_FSTRING_BOOL_RESULT = "True";

        #endregion

        #region Membership Operators

        const string CHOW_MEMBERSHIP_IN_LIST = "2 in [1, 2, 3]";
        const bool CHOW_MEMBERSHIP_IN_LIST_RESULT = true;

        const string CHOW_MEMBERSHIP_NOT_IN_LIST = "5 not in [1, 2, 3]";
        const bool CHOW_MEMBERSHIP_NOT_IN_LIST_RESULT = true;

        const string CHOW_MEMBERSHIP_IN_DICT = "\"a\" in {\"a\": 1, \"b\": 2}";
        const bool CHOW_MEMBERSHIP_IN_DICT_RESULT = true;

        const string CHOW_MEMBERSHIP_NOT_IN_DICT = "\"c\" not in {\"a\": 1}";
        const bool CHOW_MEMBERSHIP_NOT_IN_DICT_RESULT = true;

        #endregion

        #region Subscripts & Indexing (Scalar Results Only)

        const string CHOW_LIST_SUBSCRIPT = "[10, 20, 30][1]";
        const long CHOW_LIST_SUBSCRIPT_RESULT = 20L;

        const string CHOW_LIST_NEGATIVE_SUBSCRIPT = "[1, 2, 3][-1]";
        const long CHOW_LIST_NEGATIVE_SUBSCRIPT_RESULT = 3L;

        const string CHOW_DICT_SUBSCRIPT = "{\"x\": 10, \"y\": 20}[\"y\"]";
        const long CHOW_DICT_SUBSCRIPT_RESULT = 20L;

        const string CHOW_SUBSCRIPT_IN_ARITHMETIC = "[1, 2, 3][0] + [4, 5][1]";
        const long CHOW_SUBSCRIPT_IN_ARITHMETIC_RESULT = 6L;

        // --- Python-parity gaps: FEATURE NOT IMPLEMENTED (VM has no branch for the str type) ---
        // VirtualMachine.EvaluateSubscript/EvaluateSubscriptSlice/EvaluateIn only handle List/Dict,
        // so every str case below throws TypeException.
        const string CHOW_STRING_INDEX = "\"hello\"[0]";
        const string CHOW_STRING_INDEX_RESULT = "h";

        const string CHOW_STRING_SLICE = "\"hello\"[1:3]";
        const string CHOW_STRING_SLICE_RESULT = "el";

        const string CHOW_STRING_MEMBERSHIP_IN = "\"ell\" in \"hello\"";
        const bool CHOW_STRING_MEMBERSHIP_IN_RESULT = true;

        const string CHOW_STRING_MEMBERSHIP_NOT_IN = "\"z\" not in \"hello\"";
        const bool CHOW_STRING_MEMBERSHIP_NOT_IN_RESULT = true;

        #endregion

        #region Boolean to Integer Conversion

        const string CHOW_BOOL_SUM = "True + True";
        const long CHOW_BOOL_SUM_RESULT = 2L;

        const string CHOW_BOOL_INT_SUM = "True + 1";
        const long CHOW_BOOL_INT_SUM_RESULT = 2L;

        const string CHOW_BOOL_INT_PRODUCT = "False * 5";
        const long CHOW_BOOL_INT_PRODUCT_RESULT = 0L;

        const string CHOW_BOOL_FLOAT_PRODUCT = "True * 3.0";
        const double CHOW_BOOL_FLOAT_PRODUCT_RESULT = 3.0;

        #endregion

        #endregion
        
        #region 1.2) Methods
        
        [TestCaseSource(nameof(AddExpressionStatementCases))]
        public ChowValue Execute_ExpressionStatement_ReturnsCorrectValue(string sourceCode)
        {
            return ChowEngine.Execute(sourceCode);
        }
        
        static IEnumerable<TestCaseData> AddExpressionStatementCases()
        {
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
            yield return new TestCaseData(CHOW_NESTED_PARENS)
                .Returns(new ChowValue(CHOW_NESTED_PARENS_RESULT));
            yield return new TestCaseData(CHOW_EXPONENT_ADD_MULTIPLY)
                .Returns(new ChowValue(CHOW_EXPONENT_ADD_MULTIPLY_RESULT));
            yield return new TestCaseData(CHOW_MULTIPLY_EXPONENT_PRECEDENCE)
                .Returns(new ChowValue(CHOW_MULTIPLY_EXPONENT_PRECEDENCE_RESULT));
            yield return new TestCaseData(CHOW_SUBTRACTION_LEFT_ASSOCIATIVE)
                .Returns(new ChowValue(CHOW_SUBTRACTION_LEFT_ASSOCIATIVE_RESULT));
            yield return new TestCaseData(CHOW_NEGATIVE_MODULO)
                .Returns(new ChowValue(CHOW_NEGATIVE_MODULO_RESULT));
            yield return new TestCaseData(CHOW_NEGATIVE_FLOOR_DIVISION)
                .Returns(new ChowValue(CHOW_NEGATIVE_FLOOR_DIVISION_RESULT));
            yield return new TestCaseData(CHOW_SUBTRACT_NEGATIVE)
                .Returns(new ChowValue(CHOW_SUBTRACT_NEGATIVE_RESULT));
            yield return new TestCaseData(CHOW_FLOAT_DIVISION)
                .Returns(new ChowValue(CHOW_FLOAT_DIVISION_RESULT));
            yield return new TestCaseData(CHOW_MIXED_INT_FLOAT_SUM)
                .Returns(new ChowValue(CHOW_MIXED_INT_FLOAT_SUM_RESULT));
            yield return new TestCaseData(CHOW_NEGATIVE_EXPONENT)
                .Returns(new ChowValue(CHOW_NEGATIVE_EXPONENT_RESULT));
            yield return new TestCaseData(CHOW_INT_TRUE_DIVISION)
                .Returns(new ChowValue(CHOW_INT_TRUE_DIVISION_RESULT));
            yield return new TestCaseData(CHOW_FLOAT_PRECISION)
                .Returns(new ChowValue(CHOW_FLOAT_PRECISION_RESULT));
            yield return new TestCaseData(CHOW_FLOAT_FLOOR_DIVISION)
                .Returns(new ChowValue(CHOW_FLOAT_FLOOR_DIVISION_RESULT));
            yield return new TestCaseData(CHOW_FLOAT_MODULO)
                .Returns(new ChowValue(CHOW_FLOAT_MODULO_RESULT));
            yield return new TestCaseData(CHOW_INT_FLOAT_PRODUCT)
                .Returns(new ChowValue(CHOW_INT_FLOAT_PRODUCT_RESULT));
            yield return new TestCaseData(CHOW_BOOL_SUM)
                .Returns(new ChowValue(CHOW_BOOL_SUM_RESULT));
            yield return new TestCaseData(CHOW_BOOL_INT_SUM)
                .Returns(new ChowValue(CHOW_BOOL_INT_SUM_RESULT));
            yield return new TestCaseData(CHOW_BOOL_INT_PRODUCT)
                .Returns(new ChowValue(CHOW_BOOL_INT_PRODUCT_RESULT));
            yield return new TestCaseData(CHOW_BOOL_FLOAT_PRODUCT)
                .Returns(new ChowValue(CHOW_BOOL_FLOAT_PRODUCT_RESULT));
            yield return new TestCaseData(CHOW_UNARY_NEGATION)
                .Returns(new ChowValue(CHOW_UNARY_NEGATION_RESULT));
            yield return new TestCaseData(CHOW_STRING_CONCATENATION)
                .Returns(new ChowValue(CHOW_STRING_CONCATENATION_RESULT));
            yield return new TestCaseData(CHOW_STRING_REPETITION)
                .Returns(new ChowValue(CHOW_STRING_REPETITION_RESULT));
            yield return new TestCaseData(CHOW_STRING_CONCATENATION_CHAIN)
                .Returns(new ChowValue(CHOW_STRING_CONCATENATION_CHAIN_RESULT));
            yield return new TestCaseData(CHOW_STRING_REPETITION_SYMBOL)
                .Returns(new ChowValue(CHOW_STRING_REPETITION_SYMBOL_RESULT));
            yield return new TestCaseData(CHOW_INT_STRING_REPETITION)
                .Returns(new ChowValue(CHOW_INT_STRING_REPETITION_RESULT));
            yield return new TestCaseData(CHOW_FSTRING_EXPRESSION)
                .Returns(new ChowValue(CHOW_FSTRING_EXPRESSION_RESULT));
            yield return new TestCaseData(CHOW_FSTRING_WITH_TEXT)
                .Returns(new ChowValue(CHOW_FSTRING_WITH_TEXT_RESULT));
            yield return new TestCaseData(CHOW_FSTRING_BOOL)
                .Returns(new ChowValue(CHOW_FSTRING_BOOL_RESULT));
            yield return new TestCaseData(CHOW_COMPARISON_GREATER)
                .Returns(new ChowValue(CHOW_COMPARISON_GREATER_RESULT));
            yield return new TestCaseData(CHOW_CHAINED_COMPARISON)
                .Returns(new ChowValue(CHOW_CHAINED_COMPARISON_RESULT));
            yield return new TestCaseData(CHOW_EQUALITY_INT_FLOAT)
                .Returns(new ChowValue(CHOW_EQUALITY_INT_FLOAT_RESULT));
            yield return new TestCaseData(CHOW_CHAINED_COMPARISON_GREATER)
                .Returns(new ChowValue(CHOW_CHAINED_COMPARISON_GREATER_RESULT));
            yield return new TestCaseData(CHOW_CHAINED_COMPARISON_FALSE)
                .Returns(new ChowValue(CHOW_CHAINED_COMPARISON_FALSE_RESULT));
            yield return new TestCaseData(CHOW_EQUALITY_BOOL_INT)
                .Returns(new ChowValue(CHOW_EQUALITY_BOOL_INT_RESULT));
            yield return new TestCaseData(CHOW_EQUALITY_INT_STRING)
                .Returns(new ChowValue(CHOW_EQUALITY_INT_STRING_RESULT));
            yield return new TestCaseData(CHOW_INEQUALITY_INT_FLOAT)
                .Returns(new ChowValue(CHOW_INEQUALITY_INT_FLOAT_RESULT));
            yield return new TestCaseData(CHOW_STRING_COMPARISON)
                .Returns(new ChowValue(CHOW_STRING_COMPARISON_RESULT));
            yield return new TestCaseData(CHOW_BOOLEAN_AND)
                .Returns(new ChowValue(CHOW_BOOLEAN_AND_RESULT));
            yield return new TestCaseData(CHOW_BOOLEAN_OR_PRECEDENCE)
                .Returns(new ChowValue(CHOW_BOOLEAN_OR_PRECEDENCE_RESULT));
            yield return new TestCaseData(CHOW_LOGICAL_NOT)
                .Returns(new ChowValue(CHOW_LOGICAL_NOT_RESULT));
            yield return new TestCaseData(CHOW_DOUBLE_NOT)
                .Returns(new ChowValue(CHOW_DOUBLE_NOT_RESULT));
            yield return new TestCaseData(CHOW_NOT_COMPARISON_PRECEDENCE)
                .Returns(new ChowValue(CHOW_NOT_COMPARISON_PRECEDENCE_RESULT));
            yield return new TestCaseData(CHOW_AND_RETURNS_OPERAND)
                .Returns(new ChowValue(CHOW_AND_RETURNS_OPERAND_RESULT));
            yield return new TestCaseData(CHOW_OR_RETURNS_OPERAND)
                .Returns(new ChowValue(CHOW_OR_RETURNS_OPERAND_RESULT));
            yield return new TestCaseData(CHOW_OR_RETURNS_STRING)
                .Returns(new ChowValue(CHOW_OR_RETURNS_STRING_RESULT));
            yield return new TestCaseData(CHOW_AND_RETURNS_FALSY)
                .Returns(new ChowValue(CHOW_AND_RETURNS_FALSY_RESULT));
            yield return new TestCaseData(CHOW_MEMBERSHIP_IN_LIST)
                .Returns(new ChowValue(CHOW_MEMBERSHIP_IN_LIST_RESULT));
            yield return new TestCaseData(CHOW_MEMBERSHIP_NOT_IN_LIST)
                .Returns(new ChowValue(CHOW_MEMBERSHIP_NOT_IN_LIST_RESULT));
            yield return new TestCaseData(CHOW_MEMBERSHIP_IN_DICT)
                .Returns(new ChowValue(CHOW_MEMBERSHIP_IN_DICT_RESULT));
            yield return new TestCaseData(CHOW_MEMBERSHIP_NOT_IN_DICT)
                .Returns(new ChowValue(CHOW_MEMBERSHIP_NOT_IN_DICT_RESULT));
            yield return new TestCaseData(CHOW_LIST_SUBSCRIPT)
                .Returns(new ChowValue(CHOW_LIST_SUBSCRIPT_RESULT));
            yield return new TestCaseData(CHOW_LIST_NEGATIVE_SUBSCRIPT)
                .Returns(new ChowValue(CHOW_LIST_NEGATIVE_SUBSCRIPT_RESULT));
            yield return new TestCaseData(CHOW_DICT_SUBSCRIPT)
                .Returns(new ChowValue(CHOW_DICT_SUBSCRIPT_RESULT));
            yield return new TestCaseData(CHOW_SUBSCRIPT_IN_ARITHMETIC)
                .Returns(new ChowValue(CHOW_SUBSCRIPT_IN_ARITHMETIC_RESULT));
            yield return new TestCaseData(CHOW_REDUNDANT_PARENS)
                .Returns(new ChowValue(CHOW_REDUNDANT_PARENS_RESULT));
            yield return new TestCaseData(CHOW_SHALLOW_NESTED_PRODUCT)
                .Returns(new ChowValue(CHOW_SHALLOW_NESTED_PRODUCT_RESULT));
            yield return new TestCaseData(CHOW_SPACED_PARENS)
                .Returns(new ChowValue(CHOW_SPACED_PARENS_RESULT));
            yield return new TestCaseData(CHOW_REDUNDANT_PARENS_LITERAL)
                .Returns(new ChowValue(CHOW_REDUNDANT_PARENS_LITERAL_RESULT));
            yield return new TestCaseData(CHOW_ADJACENT_GROUPS_PRODUCT)
                .Returns(new ChowValue(CHOW_ADJACENT_GROUPS_PRODUCT_RESULT));
            yield return new TestCaseData(CHOW_ADJACENT_GROUPS_SUBTRACTION)
                .Returns(new ChowValue(CHOW_ADJACENT_GROUPS_SUBTRACTION_RESULT));
            yield return new TestCaseData(CHOW_PARENTHESIZED_EXPONENT)
                .Returns(new ChowValue(CHOW_PARENTHESIZED_EXPONENT_RESULT));
            yield return new TestCaseData(CHOW_NESTED_COMPARISON)
                .Returns(new ChowValue(CHOW_NESTED_COMPARISON_RESULT));
            yield return new TestCaseData(CHOW_ADJACENT_MINUS)
                .Returns(new ChowValue(CHOW_ADJACENT_MINUS_RESULT));
            yield return new TestCaseData(CHOW_STACKED_MINUS)
                .Returns(new ChowValue(CHOW_STACKED_MINUS_RESULT));
            yield return new TestCaseData(CHOW_NESTED_UNARY_PRODUCT)
                .Returns(new ChowValue(CHOW_NESTED_UNARY_PRODUCT_RESULT));
            yield return new TestCaseData(CHOW_TRIPLE_NOT)
                .Returns(new ChowValue(CHOW_TRIPLE_NOT_RESULT));
            yield return new TestCaseData(CHOW_RIGHT_NESTED_SUM)
                .Returns(new ChowValue(CHOW_RIGHT_NESTED_SUM_RESULT));
            yield return new TestCaseData(CHOW_LEFT_NESTED_SUM)
                .Returns(new ChowValue(CHOW_LEFT_NESTED_SUM_RESULT));
            yield return new TestCaseData(CHOW_DEEP_MIXED_PRECEDENCE)
                .Returns(new ChowValue(CHOW_DEEP_MIXED_PRECEDENCE_RESULT));
            yield return new TestCaseData(CHOW_DEEP_PARENS)
                .Returns(new ChowValue(CHOW_DEEP_PARENS_RESULT));
            yield return new TestCaseData(CHOW_NESTED_LIST_SUBSCRIPT)
                .Returns(new ChowValue(CHOW_NESTED_LIST_SUBSCRIPT_RESULT));
            yield return new TestCaseData(CHOW_NESTED_DICT_SUBSCRIPT)
                .Returns(new ChowValue(CHOW_NESTED_DICT_SUBSCRIPT_RESULT));
            yield return new TestCaseData(CHOW_SUBSCRIPT_EXPRESSION_INDEX)
                .Returns(new ChowValue(CHOW_SUBSCRIPT_EXPRESSION_INDEX_RESULT));
            yield return new TestCaseData(CHOW_FSTRING_NESTED_ARITHMETIC)
                .Returns(new ChowValue(CHOW_FSTRING_NESTED_ARITHMETIC_RESULT));
            yield return new TestCaseData(CHOW_FSTRING_NESTED_COMPARISON)
                .Returns(new ChowValue(CHOW_FSTRING_NESTED_COMPARISON_RESULT));
            yield return new TestCaseData(CHOW_DEEP_BOOLEAN)
                .Returns(new ChowValue(CHOW_DEEP_BOOLEAN_RESULT));
            yield return new TestCaseData(CHOW_NESTED_COMPARISON_BOOLEAN)
                .Returns(new ChowValue(CHOW_NESTED_COMPARISON_BOOLEAN_RESULT));
            yield return new TestCaseData(CHOW_EXPONENT_RIGHT_ASSOCIATIVE)
                .Returns(new ChowValue(CHOW_EXPONENT_RIGHT_ASSOCIATIVE_RESULT));
            yield return new TestCaseData(CHOW_NEGATIVE_BASE_EVEN_EXPONENT)
                .Returns(new ChowValue(CHOW_NEGATIVE_BASE_EVEN_EXPONENT_RESULT));
            yield return new TestCaseData(CHOW_NEGATIVE_BASE_ODD_EXPONENT)
                .Returns(new ChowValue(CHOW_NEGATIVE_BASE_ODD_EXPONENT_RESULT));
            yield return new TestCaseData(CHOW_MODULO_LEFT_ASSOCIATIVE)
                .Returns(new ChowValue(CHOW_MODULO_LEFT_ASSOCIATIVE_RESULT));
            yield return new TestCaseData(CHOW_FLOOR_DIVISION_LEFT_ASSOCIATIVE)
                .Returns(new ChowValue(CHOW_FLOOR_DIVISION_LEFT_ASSOCIATIVE_RESULT));
            yield return new TestCaseData(CHOW_LEADING_UNARY_NEGATION)
                .Returns(new ChowValue(CHOW_LEADING_UNARY_NEGATION_RESULT));
            yield return new TestCaseData(CHOW_LEADING_UNARY_NESTED)
                .Returns(new ChowValue(CHOW_LEADING_UNARY_NESTED_RESULT));
            yield return new TestCaseData(CHOW_STRING_INDEX)
                .Returns(new ChowValue(CHOW_STRING_INDEX_RESULT));
            yield return new TestCaseData(CHOW_STRING_SLICE)
                .Returns(new ChowValue(CHOW_STRING_SLICE_RESULT));
            yield return new TestCaseData(CHOW_STRING_MEMBERSHIP_IN)
                .Returns(new ChowValue(CHOW_STRING_MEMBERSHIP_IN_RESULT));
            yield return new TestCaseData(CHOW_STRING_MEMBERSHIP_NOT_IN)
                .Returns(new ChowValue(CHOW_STRING_MEMBERSHIP_NOT_IN_RESULT));
            yield return new TestCaseData(CHOW_CHAINED_COMPARISON_MIXED_EQUALITY)
                .Returns(new ChowValue(CHOW_CHAINED_COMPARISON_MIXED_EQUALITY_RESULT));
        }
                
        #endregion
        
        #endregion

        #region 2.) Execute - Source Code Whitespace, Empty, or Null

        const string CHOW_SOURCE_CODE_NULL = null!;
        const string CHOW_SOURCE_CODE_EMPTY = "";
        const string CHOW_SOURCE_CODE_SPACES = "   ";
        const string CHOW_SOURCE_CODE_DOUBLE_NEWLINE = "\n\n";
        const string CHOW_SOURCE_CODE_TAB = "\t";
        const string CHOW_SOURCE_CODE_LONE_COMMENT = "# Lone comment";
        const string CHOW_SOURCE_CODE_CRLF = "\r\n";
        const string CHOW_SOURCE_CODE_NEWLINE = "\n";
        const string CHOW_SOURCE_CODE_CR = "\r";
        const string CHOW_SOURCE_CODE_COMMENT_LEADING_SPACES = 
            "         # Comment with leading spaces";
        const string CHOW_SOURCE_CODE_COMMENT_BETWEEN_BLANK_LINES =
            "\n# Comment between two blank lines\n";
        const string CHOW_SOURCE_CODE_QUAD_NEWLINE = "\n\n\n\n";

        [TestCase(CHOW_SOURCE_CODE_NULL)]
        [TestCase(CHOW_SOURCE_CODE_EMPTY)]
        [TestCase(CHOW_SOURCE_CODE_SPACES)]
        [TestCase(CHOW_SOURCE_CODE_DOUBLE_NEWLINE)]
        [TestCase(CHOW_SOURCE_CODE_TAB)]
        [TestCase(CHOW_SOURCE_CODE_LONE_COMMENT)]
        [TestCase(CHOW_SOURCE_CODE_CRLF)]
        [TestCase(CHOW_SOURCE_CODE_NEWLINE)]
        [TestCase(CHOW_SOURCE_CODE_CR)]
        [TestCase(CHOW_SOURCE_CODE_COMMENT_LEADING_SPACES)]
        [TestCase(CHOW_SOURCE_CODE_COMMENT_BETWEEN_BLANK_LINES)]
        [TestCase(CHOW_SOURCE_CODE_QUAD_NEWLINE)]
        public void Execute_SourceCodeWithoutLogic_ThrowsNothing(string? sourceCode)
        {
            Assert.That(() => ChowEngine.Execute(sourceCode), Throws.Nothing);
        }

        #endregion
        
        #region 3.) Execute Variable Assignments/Declarations

        // TODO: Add tests to check for valid and invalid variable names.

        #region 3.1) Constants

        const string CHOW_ASSIGNMENT_SINGLE_VARIABLE = 
            """
            a = 1
            a
            """;
        const long CHOW_ASSIGNMENT_SINGLE_VARIABLE_RESULT = 1L;


        const string CHOW_ASSIGNMENT_SUM_OF_TWO_VARIABLES = 
            """
            a = 1
            b = 2
            a + b
            """;
        const long CHOW_ASSIGNMENT_SUM_OF_TWO_VARIABLES_RESULT = 3L;

        const string CHOW_ASSIGNMENT_SUM_OF_THREE_VARIABLES = 
            """
            a = 1
            b = 2
            c = 3
            a + b + c
            """;
        const long CHOW_ASSIGNMENT_SUM_OF_THREE_VARIABLES_RESULT = 6L;

        const string CHOW_ASSIGNMENT_CHANGE_DATA_TYPE = 
            """
            a = 1
            a = "hello"
            a
            """;
        const string CHOW_ASSIGNMENT_CHANGE_DATA_TYPE_RESULT = "hello";

        // TODO: Add ChowValue to host language conversion where None is converted to null.
        /*
        const string CHOW_ASSIGNMENT_CHANGE_DATA_TYPE_TO_NONE = 
            """
            a = 1
            a = None
            a
            """;
        const object CHOW_ASSIGNMENT_CHANGE_DATA_TYPE_TO_NONE_RESULT = null;
        */

        const string CHOW_ASSIGNMENT_ASSIGN_VARIABLE_TO_ITSELF = 
            """
            a = 1
            a = a
            a
            """;
        const long CHOW_ASSIGNMENT_ASSIGN_VARIABLE_TO_ITSELF_RESULT = 1L;

        const string CHOW_ASSIGNMENT_ASSIGN_VARIABLE_TO_OTHER_VARIABLE = 
            """
            a = 1
            b = 2
            a = b
            a
            """;
        const long CHOW_ASSIGNMENT_ASSIGN_VARIABLE_TO_OTHER_VARIABLE_RESULT = 2L;

        #endregion

        #region 3.2) Methods        

        [TestCaseSource(nameof(AddVariableAssignmentCases))]
        public ChowValue Execute_VariableAssignment_CorrectVariableValueIsSet(string sourceCode)
        {
            return ChowEngine.Execute(sourceCode);
        }

        static IEnumerable<TestCaseData> AddVariableAssignmentCases()
        {
            yield return new TestCaseData(CHOW_ASSIGNMENT_SINGLE_VARIABLE)
                .Returns(new ChowValue(CHOW_ASSIGNMENT_SINGLE_VARIABLE_RESULT));
            yield return new TestCaseData(CHOW_ASSIGNMENT_SUM_OF_TWO_VARIABLES)
                .Returns(new ChowValue(CHOW_ASSIGNMENT_SUM_OF_TWO_VARIABLES_RESULT));
            yield return new TestCaseData(CHOW_ASSIGNMENT_SUM_OF_THREE_VARIABLES)
                .Returns(new ChowValue(CHOW_ASSIGNMENT_SUM_OF_THREE_VARIABLES_RESULT));
            yield return new TestCaseData(CHOW_ASSIGNMENT_CHANGE_DATA_TYPE)
                .Returns(new ChowValue(CHOW_ASSIGNMENT_CHANGE_DATA_TYPE_RESULT));
            //yield return new TestCaseData(CHOW_ASSIGNMENT_CHANGE_DATA_TYPE_TO_NONE)
            //    .Returns(new ChowValue(CHOW_ASSIGNMENT_CHANGE_DATA_TYPE_TO_NONE_RESULT));
            yield return new TestCaseData(CHOW_ASSIGNMENT_ASSIGN_VARIABLE_TO_ITSELF)
                .Returns(new ChowValue(CHOW_ASSIGNMENT_ASSIGN_VARIABLE_TO_ITSELF_RESULT));
            yield return new TestCaseData(CHOW_ASSIGNMENT_ASSIGN_VARIABLE_TO_OTHER_VARIABLE)
                .Returns(new ChowValue(CHOW_ASSIGNMENT_ASSIGN_VARIABLE_TO_OTHER_VARIABLE_RESULT));
        }

        #endregion

        #endregion
    }
}
