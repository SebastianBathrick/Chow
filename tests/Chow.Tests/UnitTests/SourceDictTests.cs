using Chow.SourceData;

namespace Chow.Tests.UnitTests;

/// <summary>
/// Checks SourceDict's hash index against a reference model over long runs of random operations.
/// Iteration order follows Python: live keys in the order they were first inserted.
/// </summary>
public class SourceDictTests
{
    const int OperationCount = 20000;
    const int CheckInterval = 250;

    [TestCase(1, 8)] // tiny key space: constant overwrites and re-inserts of popped keys
    [TestCase(2, 300)] // frequent resizes with many holes
    [TestCase(3, 5000)] // mostly growth
    public void RandomOperations_MatchReferenceModel(int seed, int keySpace)
    {
        var rng = new Random(seed);
        var dict = new SourceDict();
        var model = new Dictionary<string, long>();
        var order = new List<string>();

        for (var step = 0; step < OperationCount; step++)
        {
            var key = RandomKey(rng, keySpace);
            var modelKey = ModelKey(key);
            var roll = rng.Next(100);

            if (roll < 55)
            {
                long value = step;
                dict.Add(key, new SourceValue(value));
                Put(model, order, modelKey, value);
            }
            else if (roll < 93)
            {
                dict.GetMethod("pop")(new[] { key, SourceValue.None });

                if (model.Remove(modelKey))
                {
                    order.Remove(modelKey);
                }
            }
            else if (roll < 94)
            {
                dict.GetMethod("clear")(null);
                model.Clear();
                order.Clear();
            }
            else if (roll < 97)
            {
                // Copy path: a clone (holes kept) or a compacting merge, depending on the holes.
                dict = SourceDict.Merge(dict, new SourceDict());
            }
            else
            {
                var other = new SourceDict();

                for (var i = 0; i < rng.Next(1, 12); i++)
                {
                    var otherKey = RandomKey(rng, keySpace);
                    long value = -step - i;
                    other.Add(otherKey, new SourceValue(value));
                    dict.Add(otherKey, new SourceValue(value));
                    Put(model, order, ModelKey(otherKey), value);
                }

                // Updating with keys it already holds must change nothing.
                dict.GetMethod("update")(new[] { new SourceValue(other) });
            }

            Assert.That(dict.Length, Is.EqualTo(model.Count), $"length after step {step}");

            if (step % CheckInterval == 0)
            {
                AssertMatchesModel(dict, model, order, keySpace, step);
            }
        }

        AssertMatchesModel(dict, model, order, keySpace, OperationCount);
    }

    static void Put(Dictionary<string, long> model, List<string> order, string modelKey, long value)
    {
        if (!model.ContainsKey(modelKey))
        {
            order.Add(modelKey);
        }

        model[modelKey] = value;
    }

    // Mixes int and str keys so str-only tables are converted along the way.
    static SourceValue RandomKey(Random rng, int keySpace)
    {
        var n = rng.Next(keySpace);
        return rng.Next(2) == 0 ? new SourceValue((long)n) : new SourceValue("k" + n);
    }

    static string ModelKey(SourceValue key)
    {
        return key.IsString ? "s" + key : "i" + key.ToLong();
    }

    static void AssertMatchesModel(
        SourceDict dict, Dictionary<string, long> model, List<string> order, int keySpace, int step)
    {
        var iterated = new List<string>();
        var iterator = dict.GetIterator();

        while (iterator.TryMoveNext(out var key))
        {
            iterated.Add(ModelKey(key));
        }

        Assert.That(iterated, Is.EqualTo(order), $"iteration order after step {step}");

        for (var n = 0; n < keySpace; n++)
        {
            foreach (var key in new[] { new SourceValue((long)n), new SourceValue("k" + n) })
            {
                var isPresent = model.TryGetValue(ModelKey(key), out var expected);

                Assert.That(dict.ContainsKey(key), Is.EqualTo(isPresent), $"membership of {key} after step {step}");

                if (isPresent)
                {
                    Assert.That(dict.GetItem(key).ToLong(), Is.EqualTo(expected), $"value of {key} after step {step}");
                }
            }
        }
    }
}
