namespace Chow.Tests.SourceCode;

[TestFixture]
public class ChowProgramTests
{
    static readonly string CollatzSourceCode = """
                                               def collatz(n):
                                                   steps = 0
                                                   while n != 1:
                                                       if n % 2 == 0:
                                                           n = n // 2
                                                       else:
                                                           n = n * 3 + 1
                                                       steps = steps + 1
                                                   print(n)
                                                   return steps
                                               
                                               collatz(6)
                                               """;

    static readonly ChowValue CollatzExpectedResult = new(8);
    
    [Test]
    public void Execute_CollatzConjecture_CorrectExpressionStatement()
    {
        var returnValue = ChowEngine.Execute(CollatzSourceCode);
        
        Assert.That(returnValue, Is.EqualTo(CollatzExpectedResult));
    }
}
