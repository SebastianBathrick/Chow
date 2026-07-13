using Chow;
using Chow.Tokens;
using Chow.Code;
using Chow.Interpreter.Exceptions;
using Chow.Interpreter.Lexing;

namespace Chow.Tests.UnitTests;

public class SyntaxMapsTests
{
    [Test]
    public void IsExpressionStart_TokenTypeCanBeginExpression_ReturnsTrue()
    {
        var startTypes = new[]
        {
            TokenType.Name,
            TokenType.LiteralInt,
            TokenType.LiteralFloat,
            TokenType.LiteralStr,
            TokenType.LiteralFString,
            TokenType.KeywordNone,
            TokenType.KeywordTrue,
            TokenType.KeywordFalse,
            TokenType.KeywordNot,
            TokenType.SymbolLeftParen,
            TokenType.SymbolLeftBracket,
            TokenType.SymbolMinus,
            TokenType.SymbolLeftCurly
        };

        Assert.Multiple(() =>
        {
            foreach (var type in startTypes)
            {
                Assert.That(SyntaxMaps.IsExpressionStart(type), Is.True, type.ToString());
            }
        });
    }

    [Test]
    public void IsExpressionStart_TokenTypeCannotBeginExpression_ReturnsFalse()
    {
        var nonStartTypes = new[]
        {
            TokenType.SymbolAssign,
            TokenType.SymbolPlus,
            TokenType.SymbolRightParen,
            TokenType.KeywordIf,
            TokenType.KeywordIn,
            TokenType.Newline,
            TokenType.EndOfCode
        };

        Assert.Multiple(() =>
        {
            foreach (var type in nonStartTypes)
            {
                Assert.That(SyntaxMaps.IsExpressionStart(type), Is.False, type.ToString());
            }
        });
    }

    [Test]
    public void IsComparisonOperator_ComparisonTokenType_ReturnsTrue()
    {
        var comparisonTypes = new[]
        {
            TokenType.SymbolEqualTo,
            TokenType.SymbolNotEqual,
            TokenType.SymbolLess,
            TokenType.SymbolGreater,
            TokenType.SymbolLessEqual,
            TokenType.SymbolGreaterEqual,
            TokenType.KeywordIn
        };

        Assert.Multiple(() =>
        {
            foreach (var type in comparisonTypes)
            {
                Assert.That(SyntaxMaps.IsComparisonOperator(type), Is.True, type.ToString());
            }
        });
    }

    [Test]
    public void IsComparisonOperator_NonComparisonTokenType_ReturnsFalse()
    {
        var nonComparisonTypes = new[]
        {
            TokenType.SymbolAssign,
            TokenType.SymbolPlus,
            TokenType.KeywordNot,
            TokenType.KeywordAnd,
            TokenType.Name
        };

        Assert.Multiple(() =>
        {
            foreach (var type in nonComparisonTypes)
            {
                Assert.That(SyntaxMaps.IsComparisonOperator(type), Is.False, type.ToString());
            }
        });
    }

    [Test]
    public void ToBinaryOperator_OperatorTokenType_ReturnsMappedOperator()
    {
        var expectedMappings = new Dictionary<TokenType, Operator>
        {
            [TokenType.SymbolPlus] = Operator.Add,
            [TokenType.SymbolMinus] = Operator.Subtract,
            [TokenType.SymbolMultiply] = Operator.Multiply,
            [TokenType.SymbolDivide] = Operator.Divide,
            [TokenType.SymbolPercent] = Operator.Modulus,
            [TokenType.SymbolExponent] = Operator.Exponentiate,
            [TokenType.SymbolFloorDivide] = Operator.FloorDivide,
            [TokenType.SymbolEqualTo] = Operator.Equal,
            [TokenType.SymbolNotEqual] = Operator.NotEqual,
            [TokenType.SymbolLess] = Operator.Less,
            [TokenType.SymbolGreater] = Operator.Greater,
            [TokenType.SymbolLessEqual] = Operator.LessEqual,
            [TokenType.SymbolGreaterEqual] = Operator.GreaterEqual,
            [TokenType.KeywordAnd] = Operator.And,
            [TokenType.KeywordOr] = Operator.Or,
            [TokenType.SymbolPipe] = Operator.BinaryOr,
            [TokenType.KeywordIn] = Operator.In
        };

        Assert.Multiple(() =>
        {
            foreach (var mapping in expectedMappings)
            {
                Assert.That(
                    SyntaxMaps.ToBinaryOperator(mapping.Key),
                    Is.EqualTo(mapping.Value),
                    mapping.Key.ToString());
            }
        });
    }

    [Test]
    public void ToBinaryOperator_NonOperatorTokenType_ThrowsUnreachableException()
    {
        Assert.That(
            () => SyntaxMaps.ToBinaryOperator(TokenType.Name),
            Throws.TypeOf<UnreachableException>());
    }
}
