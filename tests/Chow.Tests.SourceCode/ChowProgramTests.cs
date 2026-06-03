namespace Chow.Tests.SourceCode;

[TestFixture]
public class ChowProgramTests
{
    #region Source Code

    static readonly string CollatzSourceCode = """
                                               def collatz(n):
                                                   steps = 0
                                                   while n != 1:
                                                       if n % 2 == 0:
                                                           n = n // 2
                                                       else:
                                                           n = n * 3 + 1
                                                       steps = steps + 1
                                                   return steps

                                               collatz(6)
                                               """;

    static readonly string FizzBuzzSourceCode = """
                                                result = ""
                                                for i in range(1, 16):
                                                    if i % 15 == 0:
                                                        result = result + "FizzBuzz"
                                                    elif i % 3 == 0:
                                                        result = result + "Fizz"
                                                    elif i % 5 == 0:
                                                        result = result + "Buzz"
                                                    else:
                                                        result = result + str(i)
                                                    result = result + ","
                                                result
                                                """;

    static readonly string CaesarCipherSourceCode = """
                                                     alpha = ["a","b","c","d","e","f","g","h","i","j","k","l","m",
                                                              "n","o","p","q","r","s","t","u","v","w","x","y","z"]

                                                     def caesar(text, shift):
                                                         result = ""
                                                         for ch in text:
                                                             if ch in alpha:
                                                                 idx = 0
                                                                 for i in range(26):
                                                                     if alpha[i] == ch:
                                                                         idx = i
                                                                 result = result + alpha[(idx + shift) % 26]
                                                             else:
                                                                 result = result + ch
                                                         return result

                                                     encrypted = caesar("hello", 3)
                                                     caesar(encrypted, -3)
                                                     """;

    static readonly string SieveSourceCode = """
                                             def sieve(limit):
                                                 candidates = list(range(2, limit + 1))
                                                 primes = []
                                                 while len(candidates) > 0:
                                                     p = candidates[0]
                                                     primes = primes + [p]
                                                     remaining = []
                                                     for n in candidates:
                                                         if n % p != 0:
                                                             remaining = remaining + [n]
                                                     candidates = remaining
                                                 return primes

                                             str(sieve(30))
                                             """;

    static readonly string BankAccountSourceCode = """
                                                    def make_account(initial):
                                                        balance = initial

                                                        def deposit(amount):
                                                            nonlocal balance
                                                            balance = balance + amount
                                                            return balance

                                                        def withdraw(amount):
                                                            nonlocal balance
                                                            if amount > balance:
                                                                return "error: insufficient funds"
                                                            balance = balance - amount
                                                            return balance

                                                        def get_balance():
                                                            return balance

                                                        return {"deposit": deposit, "withdraw": withdraw, "balance": get_balance}

                                                    acct = make_account(100)
                                                    acct["withdraw"](40)
                                                    acct["deposit"](60)
                                                    acct["withdraw"](200)
                                                    acct["balance"]()
                                                    """;

    static readonly string WordFrequencySourceCode = """
                                                      def word_freq(words):
                                                          freq = {}
                                                          for word in words:
                                                              if word in freq:
                                                                  freq[word] = freq[word] + 1
                                                              else:
                                                                  freq[word] = 1
                                                          return freq

                                                      sentence = ["the", "cat", "sat", "on", "the", "mat", "the", "cat"]
                                                      counts = word_freq(sentence)
                                                      counts["the"]
                                                      """;

    static readonly string BinarySearchHitSourceCode = """
                                                        def binary_search(lst, target):
                                                            lo = 0
                                                            hi = len(lst) - 1
                                                            while lo <= hi:
                                                                mid = (lo + hi) // 2
                                                                if lst[mid] == target:
                                                                    return mid
                                                                elif lst[mid] < target:
                                                                    lo = mid + 1
                                                                else:
                                                                    hi = mid - 1
                                                            return -1

                                                        sorted_list = [1, 3, 5, 7, 9, 11, 14, 18, 22, 30]
                                                        binary_search(sorted_list, 14)
                                                        """;

    static readonly string BinarySearchMissSourceCode = """
                                                         def binary_search(lst, target):
                                                             lo = 0
                                                             hi = len(lst) - 1
                                                             while lo <= hi:
                                                                 mid = (lo + hi) // 2
                                                                 if lst[mid] == target:
                                                                     return mid
                                                                 elif lst[mid] < target:
                                                                     lo = mid + 1
                                                                 else:
                                                                     hi = mid - 1
                                                             return -1

                                                         sorted_list = [1, 3, 5, 7, 9, 11, 14, 18, 22, 30]
                                                         binary_search(sorted_list, 99)
                                                         """;

    static readonly string ClosureCounterSourceCode = """
                                                       def make_counter():
                                                           count = 0
                                                           def increment():
                                                               nonlocal count
                                                               count = count + 1
                                                               return count
                                                           return increment

                                                       counter = make_counter()
                                                       other = make_counter()
                                                       counter()
                                                       counter()
                                                       counter()
                                                       other()
                                                       """;

    #endregion

    #region Expected Results

    static readonly ChowValue CollatzExpectedResult = new(8L);
    static readonly ChowValue FizzBuzzExpectedResult = new("1,2,Fizz,4,Buzz,Fizz,7,8,Fizz,Buzz,11,Fizz,13,14,FizzBuzz,");
    static readonly ChowValue CaesarCipherExpectedResult = new("hello");
    static readonly ChowValue SieveExpectedResult = new("[2, 3, 5, 7, 11, 13, 17, 19, 23, 29]");
    static readonly ChowValue BankAccountFinalBalanceExpectedResult = new(120L);
    static readonly ChowValue WordFrequencyExpectedResult = new(3L);
    static readonly ChowValue BinarySearchHitExpectedResult = new(6L);
    static readonly ChowValue BinarySearchMissExpectedResult = new(-1L);
    static readonly ChowValue ClosureCounterExpectedResult = new(1L);

    #endregion

    #region Tests

    [Test]
    public void Execute_CollatzConjecture_CorrectStepCount()
    {
        var returnValue = ChowEngine.Execute(CollatzSourceCode);

        Assert.That(returnValue, Is.EqualTo(CollatzExpectedResult));
    }

    [Test]
    public void Execute_FizzBuzz_CorrectOutputString()
    {
        var returnValue = ChowEngine.Execute(FizzBuzzSourceCode);

        Assert.That(returnValue, Is.EqualTo(FizzBuzzExpectedResult));
    }

    [Test]
    public void Execute_CaesarCipher_RoundTripRestoresOriginal()
    {
        var returnValue = ChowEngine.Execute(CaesarCipherSourceCode);

        Assert.That(returnValue, Is.EqualTo(CaesarCipherExpectedResult));
    }

    [Test]
    public void Execute_Sieve_CorrectPrimeList()
    {
        var returnValue = ChowEngine.Execute(SieveSourceCode);

        Assert.That(returnValue, Is.EqualTo(SieveExpectedResult));
    }

    [Test]
    public void Execute_BankAccount_CorrectFinalBalance()
    {
        var returnValue = ChowEngine.Execute(BankAccountSourceCode);

        Assert.That(returnValue, Is.EqualTo(BankAccountFinalBalanceExpectedResult));
    }

    [Test]
    public void Execute_WordFrequency_CorrectCountForRepeatedWord()
    {
        var returnValue = ChowEngine.Execute(WordFrequencySourceCode);

        Assert.That(returnValue, Is.EqualTo(WordFrequencyExpectedResult));
    }

    [Test]
    public void Execute_BinarySearch_ReturnsCorrectIndexForHit()
    {
        var returnValue = ChowEngine.Execute(BinarySearchHitSourceCode);

        Assert.That(returnValue, Is.EqualTo(BinarySearchHitExpectedResult));
    }

    [Test]
    public void Execute_BinarySearch_ReturnsNegativeOneForMiss()
    {
        var returnValue = ChowEngine.Execute(BinarySearchMissSourceCode);

        Assert.That(returnValue, Is.EqualTo(BinarySearchMissExpectedResult));
    }

    [Test]
    public void Execute_ClosureCounter_IndependentCountersDoNotShareState()
    {
        var returnValue = ChowEngine.Execute(ClosureCounterSourceCode);

        Assert.That(returnValue, Is.EqualTo(ClosureCounterExpectedResult));
    }

    #endregion
}