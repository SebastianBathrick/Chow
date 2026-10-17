using Chow.Code;
using Chow.SourceData;

namespace Chow.Tests.UnitTests;

/// <summary>
/// Checks SourceSet's CPython-style table against a reference model over long runs of random
/// operations. Iteration order is covered by the CPython differential tests; this checks contents.
/// </summary>
public class SourceSetTests
{
    const int OperationCount = 20000;
    const int CheckInterval = 250;

    [TestCase(1, 8)] // tiny key space: constant re-adds and dummy reuse
    [TestCase(2, 300)] // frequent resizes with many dummies
    [TestCase(3, 5000)] // mostly growth
    public void RandomOperations_MatchReferenceModel(int seed, int keySpace)
    {
        var rng = new Random(seed);
        var set = new SourceSet(isFrozen: false);
        var model = new HashSet<string>();

        for (var step = 0; step < OperationCount; step++)
        {
            var key = RandomKey(rng, keySpace);
            var roll = rng.Next(100);

            if (roll < 55)
            {
                set.Add(key);
                model.Add(ModelKey(key));
            }
            else if (roll < 90)
            {
                set.GetMethod("discard")(new[] { key });
                model.Remove(ModelKey(key));
            }
            else if (roll < 95 && set.Length > 0)
            {
                model.Remove(ModelKey(set.GetMethod("pop")(null)));
            }
            else if (roll < 96)
            {
                set.GetMethod("clear")(null);
                model.Clear();
            }
            else
            {
                // Copy path: a slot-for-slot copy or a clean reinsert, depending on sizes and dummies.
                set = (SourceSet)set.GetMethod("copy")(null).ToObject();
            }

            Assert.That(set.Length, Is.EqualTo(model.Count), $"length after step {step}");

            if (step % CheckInterval == 0)
            {
                AssertMatchesModel(set, model, keySpace, step);
            }
        }

        AssertMatchesModel(set, model, keySpace, OperationCount);
    }

    [TestCase(4)]
    [TestCase(5)]
    public void AlgebraOperations_MatchReferenceModel(int seed)
    {
        var rng = new Random(seed);

        for (var trial = 0; trial < 300; trial++)
        {
            // Small key spaces give heavy overlap; sizes span the difference size heuristics.
            var keySpace = rng.Next(1, 300);
            var (left, leftModel) = RandomSet(rng, keySpace, rng.Next(0, 80));
            var (right, rightModel) = RandomSet(rng, keySpace, rng.Next(0, 80));
            var rightSet = new SourceValue(right);
            var rightList = ListOf(right);

            var union = new HashSet<string>(leftModel);
            union.UnionWith(rightModel);
            var intersection = new HashSet<string>(leftModel);
            intersection.IntersectWith(rightModel);
            var difference = new HashSet<string>(leftModel);
            difference.ExceptWith(rightModel);
            var symmetric = new HashSet<string>(leftModel);
            symmetric.SymmetricExceptWith(rightModel);

            // Each operation through a set operand and through a plain iterable, which take different paths.
            foreach (var other in new[] { rightSet, rightList })
            {
                AssertContents(Call(left, "union", other), union, "union", trial);
                AssertContents(left.Intersection(other), intersection, "intersection", trial);
                AssertContents(left.Difference(other), difference, "difference", trial);
                AssertContents(left.SymmetricDifference(other), symmetric, "symmetric_difference", trial);

                AssertContents(Update(left, "intersection_update", other), intersection, "intersection_update", trial);
                AssertContents(Update(left, "difference_update", other), difference, "difference_update", trial);
                AssertContents(Update(left, "symmetric_difference_update", other), symmetric,
                    "symmetric_difference_update", trial);
            }

            Assert.That(left.Compare(right, Operator.LessEqual), Is.EqualTo(leftModel.IsSubsetOf(rightModel)), $"<= in trial {trial}");
            Assert.That(left.Compare(right, Operator.Less), Is.EqualTo(leftModel.IsProperSubsetOf(rightModel)), $"< in trial {trial}");
            Assert.That(left.Compare(right, Operator.GreaterEqual), Is.EqualTo(leftModel.IsSupersetOf(rightModel)), $">= in trial {trial}");
            Assert.That(left.Compare(right, Operator.Greater), Is.EqualTo(leftModel.IsProperSupersetOf(rightModel)), $"> in trial {trial}");
        }
    }

    // A set built by adds and discards, so its table holds dummies as well as elements.
    static (SourceSet Set, HashSet<string> Model) RandomSet(Random rng, int keySpace, int operationCount)
    {
        var set = new SourceSet(isFrozen: false);
        var model = new HashSet<string>();

        for (var i = 0; i < operationCount; i++)
        {
            var key = RandomKey(rng, keySpace);

            if (rng.Next(4) == 0)
            {
                set.GetMethod("discard")(new[] { key });
                model.Remove(ModelKey(key));
            }
            else
            {
                set.Add(key);
                model.Add(ModelKey(key));
            }
        }

        return (set, model);
    }

    static SourceValue ListOf(SourceSet set)
    {
        var list = new SourceList();
        var iterator = set.GetIterator();

        while (iterator.TryMoveNext(out var key))
        {
            list.AppendItem(key);
        }

        return new SourceValue(list);
    }

    static SourceSet Call(SourceSet set, string method, SourceValue argument)
    {
        return (SourceSet)set.GetMethod(method)(new[] { argument }).ToObject();
    }

    // Runs an in-place method on a copy, leaving the original for the next operation.
    static SourceSet Update(SourceSet set, string method, SourceValue argument)
    {
        var copy = set.Copy();
        copy.GetMethod(method)(new[] { argument });
        return copy;
    }

    static void AssertContents(SourceSet set, HashSet<string> expected, string operation, int trial)
    {
        var actual = new List<string>();
        var iterator = set.GetIterator();

        while (iterator.TryMoveNext(out var key))
        {
            actual.Add(ModelKey(key));
        }

        Assert.That(actual, Is.EquivalentTo(expected), $"{operation} in trial {trial}");
        Assert.That(set.Length, Is.EqualTo(expected.Count), $"{operation} length in trial {trial}");
    }

    // Mixes int and str keys; equal numbers of different types (1, 1.0, True) are one element.
    static SourceValue RandomKey(Random rng, int keySpace)
    {
        var n = rng.Next(keySpace);

        switch (rng.Next(3))
        {
            case 0:
                return new SourceValue((long)n);
            case 1:
                return new SourceValue((double)n);
            default:
                return new SourceValue("k" + n);
        }
    }

    static string ModelKey(SourceValue key)
    {
        return key.IsString ? "s" + key : "n" + (long)key.ToDouble();
    }

    static void AssertMatchesModel(SourceSet set, HashSet<string> model, int keySpace, int step)
    {
        var iterated = new List<string>();
        var iterator = set.GetIterator();

        while (iterator.TryMoveNext(out var key))
        {
            iterated.Add(ModelKey(key));
        }

        Assert.That(iterated, Is.EquivalentTo(model), $"elements after step {step}");

        for (var n = 0; n < keySpace; n++)
        {
            foreach (var key in new[] { new SourceValue((long)n), new SourceValue("k" + n) })
            {
                Assert.That(set.Contains(key), Is.EqualTo(model.Contains(ModelKey(key))),
                    $"membership of {key} after step {step}");
            }
        }
    }
}
