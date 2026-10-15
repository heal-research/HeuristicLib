using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Routes each input to a weighted choice, hands every choice its inputs as one batch and returns the results in input
/// order.
/// </summary>
/// <remarks>
/// Weights use <see cref="WeightedIndexSampler"/> semantics and are compiled once. Routing assigns every input before
/// handling any batch, then handles the batches in choice order and skips choices that received no inputs. When every
/// input goes to the same choice, that choice receives the original inputs. Each batch must produce exactly one result
/// per input or requested output, in batch order. The supplied random source is used for routing and is not forked.
/// </remarks>
public sealed class WeightedRouter<TChoice>
{
    private readonly ImmutableArray<TChoice> choices;
    private readonly WeightedIndexSampler sampler;

    public WeightedRouter(IReadOnlyList<TChoice> choices, IReadOnlyList<double>? weights = null)
    {
        this.choices = [.. choices];
        sampler = new WeightedIndexSampler(choices.Count, weights);
    }

    /// <summary>Routes each input to a choice and returns the results in input order.</summary>
    public IReadOnlyList<TOutput> Route<TInput, TOutput>(IReadOnlyList<TInput> inputs, IRandomNumberGenerator random, Func<TChoice, IReadOnlyList<TInput>, IReadOnlyList<TOutput>> handle)
    {
        if (inputs.Count == 0)
            return [];

        var assignedChoices = AssignChoices(inputs.Count, random);
        if (IsSingleChoice(assignedChoices))
            return CheckCount(handle(choices[assignedChoices[0]], inputs), inputs.Count);

        var positions = GroupByChoice(assignedChoices, out var batchStarts);
        var groupedInputs = new TInput[inputs.Count];
        for (var i = 0; i < groupedInputs.Length; i++)
            groupedInputs[i] = inputs[positions[i]];

        var results = new TOutput[inputs.Count];
        for (var choice = 0; choice < choices.Length; choice++)
        {
            var start = batchStarts[choice];
            var length = batchStarts[choice + 1] - start;
            if (length > 0)
                Scatter(handle(choices[choice], new ArraySegment<TInput>(groupedInputs, start, length)), positions, start, length, results);
        }

        return results;
    }

    /// <summary>Routes each of <paramref name="count"/> requested outputs to a choice and returns them in request order.</summary>
    public IReadOnlyList<TOutput> Route<TOutput>(int count, IRandomNumberGenerator random, Func<TChoice, int, IReadOnlyList<TOutput>> handle)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        if (count == 0)
            return [];

        var assignedChoices = AssignChoices(count, random);
        if (IsSingleChoice(assignedChoices))
            return CheckCount(handle(choices[assignedChoices[0]], count), count);

        var positions = GroupByChoice(assignedChoices, out var batchStarts);
        var results = new TOutput[count];
        for (var choice = 0; choice < choices.Length; choice++)
        {
            var start = batchStarts[choice];
            var length = batchStarts[choice + 1] - start;
            if (length > 0)
                Scatter(handle(choices[choice], length), positions, start, length, results);
        }

        return results;
    }

    private int[] AssignChoices(int count, IRandomNumberGenerator random)
    {
        var assignedChoices = new int[count];
        for (var i = 0; i < count; i++)
            assignedChoices[i] = sampler.Sample(random);

        return assignedChoices;
    }

    private static bool IsSingleChoice(int[] assignedChoices) =>
        assignedChoices.AsSpan().IndexOfAnyExcept(assignedChoices[0]) < 0;

    /// <summary>
    /// Returns the positions ordered by their assigned choice. The batch of choice <c>c</c> occupies
    /// <c>[batchStarts[c], batchStarts[c + 1])</c> of the result.
    /// </summary>
    private int[] GroupByChoice(int[] assignedChoices, out int[] batchStarts)
    {
        batchStarts = new int[choices.Length + 1];
        foreach (var choice in assignedChoices)
            batchStarts[choice + 1]++;

        for (var choice = 0; choice < choices.Length; choice++)
            batchStarts[choice + 1] += batchStarts[choice];

        var nextSlots = batchStarts[..^1];
        var positions = new int[assignedChoices.Length];
        for (var position = 0; position < assignedChoices.Length; position++)
            positions[nextSlots[assignedChoices[position]]++] = position;

        return positions;
    }

    private static void Scatter<TOutput>(IReadOnlyList<TOutput> batchResults, int[] positions, int start, int length, TOutput[] results)
    {
        CheckCount(batchResults, length);
        for (var i = 0; i < length; i++)
            results[positions[start + i]] = batchResults[i];
    }

    private static IReadOnlyList<TOutput> CheckCount<TOutput>(IReadOnlyList<TOutput> batchResults, int batchSize) =>
        batchResults.Count == batchSize
            ? batchResults
            : throw new InvalidOperationException("A routed choice returned a result count that does not match its batch size.");
}

public static class WeightedRouter
{
    /// <summary>Creates a router that sends each input to one of <paramref name="choices"/> uniformly.</summary>
    public static WeightedRouter<TChoice> Create<TChoice>(params IReadOnlyList<TChoice> choices) =>
        new(choices);

    /// <summary>Creates a router that sends each input to one of <paramref name="choices"/> by its weight.</summary>
    public static WeightedRouter<TChoice> Create<TChoice>(IReadOnlyList<TChoice> choices, IReadOnlyList<double> weights) =>
        new(choices, weights);

    /// <summary>
    /// Creates a router that sends each input to <see langword="true"/> with probability <paramref name="rate"/> and to
    /// <see langword="false"/> otherwise.
    /// </summary>
    /// <remarks>
    /// The rate is applied as a threshold against a random value in <c>[0, 1)</c>. A rate at most zero, negative infinity
    /// and <c>NaN</c> route every input to <see langword="false"/>; a rate at least one and positive infinity route every
    /// input to <see langword="true"/>. Neither extreme draws from the random source.
    /// </remarks>
    public static WeightedRouter<bool> AtRate(double rate) => new([true, false], RateWeights(rate));

    /// <summary>
    /// Creates a router that sends each input to <paramref name="choice"/> with probability <paramref name="rate"/>
    /// and to <see langword="null"/> otherwise.
    /// </summary>
    /// <remarks>
    /// A rate at most zero, negative infinity and <c>NaN</c> route every input to <see langword="null"/>; a rate at
    /// least one and positive infinity route every input to <paramref name="choice"/>. Neither extreme draws from the
    /// random source. The handler defines the behavior of the null choice, such as passing a batch through unchanged.
    /// </remarks>
    public static WeightedRouter<TChoice?> AtRate<TChoice>(TChoice choice, double rate)
        where TChoice : class =>
        new([choice, null], RateWeights(rate));

    /// <summary>Weights that select the first of two entries with probability <paramref name="rate"/>.</summary>
    /// <remarks>
    /// <c>1 - NaN</c> is <c>NaN</c>, and two unselectable weights fall back to uniform sampling, so a <c>NaN</c> rate
    /// makes the second entry certain instead.
    /// </remarks>
    internal static double[] RateWeights(double rate) =>
        [rate, double.IsNaN(rate) ? double.PositiveInfinity : 1 - rate];
}
