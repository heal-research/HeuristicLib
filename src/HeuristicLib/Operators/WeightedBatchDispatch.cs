using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public sealed class WeightedBatchDispatch
{
    public ImmutableArray<double> Weights { get; }

    private readonly double totalWeight;
    private readonly double[] cumulativeWeights = [];
    private readonly int[] selectableIndices = [];

    public WeightedBatchDispatch(IReadOnlyList<double>? weights = null)
    {
        var immutableWeights = weights?.ToImmutableArray() ?? [];
        Weights = immutableWeights.IsDefault ? [] : immutableWeights;

        var positiveInfinityCount = 0;
        for (var i = 0; i < Weights.Length; i++)
        {
            if (double.IsPositiveInfinity(Weights[i]))
                positiveInfinityCount++;
        }

        if (positiveInfinityCount > 0)
        {
            if (positiveInfinityCount < Weights.Length)
                selectableIndices = GetSelectableIndices(static weight => double.IsPositiveInfinity(weight), positiveInfinityCount);

            return;
        }

        var positiveWeightCount = 0;
        var maximumWeight = 0.0;
        var firstPositiveWeight = 0.0;
        var allPositiveWeightsEqual = true;
        for (var i = 0; i < Weights.Length; i++)
        {
            var weight = Weights[i];
            if (double.IsNaN(weight) || weight <= 0)
                continue;

            if (positiveWeightCount == 0)
                firstPositiveWeight = weight;
            else if (!weight.Equals(firstPositiveWeight))
                allPositiveWeightsEqual = false;

            positiveWeightCount++;
            maximumWeight = Math.Max(maximumWeight, weight);
        }

        if (positiveWeightCount == 0)
            return;

        if (allPositiveWeightsEqual)
        {
            if (positiveWeightCount < Weights.Length)
                selectableIndices = GetSelectableIndices(static weight => weight > 0, positiveWeightCount);

            return;
        }

        cumulativeWeights = new double[Weights.Length];
        for (var i = 0; i < Weights.Length; i++)
        {
            var weight = Weights[i];
            if (weight > 0)
                totalWeight += weight / maximumWeight;

            cumulativeWeights[i] = totalWeight;
        }
    }

    /// <summary>
    /// Returns the weights for choosing between an operator applied at <paramref name="rate"/> and its unchanged
    /// alternative, in that order.
    /// </summary>
    /// <remarks>
    /// A rate of <c>NaN</c> or at most zero never applies the operator; a rate of at least one always applies it.
    /// </remarks>
    public static ImmutableArray<double> GetRateWeights(double rate) =>
        [rate, double.IsNaN(rate) ? double.PositiveInfinity : 1 - rate];

    public IReadOnlyList<TOutput> Dispatch<TInput, TOutput, TOperator>(IReadOnlyList<TInput> inputs, IReadOnlyList<TOperator> operators, IRandomNumberGenerator random, Func<TOperator, IReadOnlyList<TInput>, IReadOnlyList<TOutput>> invokeBatch)
        => Dispatch(inputs, operators, random, invokeBatch, static (operator_, batch, invoke) => invoke(operator_, batch));

    public IReadOnlyList<TOutput> Dispatch<TInput, TOutput, TOperator, TState>(IReadOnlyList<TInput> inputs, IReadOnlyList<TOperator> operators, IRandomNumberGenerator random, TState state, Func<TOperator, IReadOnlyList<TInput>, TState, IReadOnlyList<TOutput>> invokeBatch)
    {
        if (Weights.Length > 0 && operators.Count != Weights.Length)
            throw new ArgumentException("Weights must have the same length as operators.", nameof(operators));

        if (inputs.Count == 0)
            return [];

        var operatorAssignments = new int[inputs.Count];
        var operatorCounts = new int[operators.Count];
        var operatorStarts = new int[operators.Count + 1];
        for (var i = 0; i < inputs.Count; i++)
        {
            var operatorIndex = ChooseOperatorUnchecked(random, operators.Count);
            operatorAssignments[i] = operatorIndex;
            operatorCounts[operatorIndex]++;
        }

        for (var i = 0; i < operators.Count; i++)
            operatorStarts[i + 1] = operatorStarts[i] + operatorCounts[i];

        operatorStarts.AsSpan(0, operators.Count).CopyTo(operatorCounts);
        var groupedInputs = new TInput[inputs.Count];
        var groupedInputIndices = new int[inputs.Count];
        for (var i = 0; i < inputs.Count; i++)
        {
            var operatorIndex = operatorAssignments[i];
            var groupedIndex = operatorCounts[operatorIndex]++;
            groupedInputs[groupedIndex] = inputs[i];
            groupedInputIndices[groupedIndex] = i;
        }

        var results = new TOutput[inputs.Count];
        for (var i = 0; i < operators.Count; i++)
        {
            var batchStart = operatorStarts[i];
            var batchCount = operatorStarts[i + 1] - batchStart;
            if (batchCount == 0)
                continue;

            IReadOnlyList<TInput> inputBatch = new ArraySegment<TInput>(groupedInputs, batchStart, batchCount);
            var batchResults = invokeBatch(operators[i], inputBatch, state);
            if (batchResults.Count != batchCount)
                throw new InvalidOperationException("A weighted batch operator returned a result count that does not match its input count.");

            for (var j = 0; j < batchResults.Count; j++)
                results[groupedInputIndices[batchStart + j]] = batchResults[j];
        }

        return results;
    }

    public int ChooseOperator(IRandomNumberGenerator random, int operatorCount)
    {
        if (Weights.Length > 0 && operatorCount != Weights.Length)
            throw new ArgumentException("Weights must have the same length as operators.", nameof(operatorCount));

        return ChooseOperatorUnchecked(random, operatorCount);
    }

    private int ChooseOperatorUnchecked(IRandomNumberGenerator random, int operatorCount)
    {
        if (cumulativeWeights.Length > 0)
            return ChooseWeightedOperator(random.NextDouble());

        if (selectableIndices.Length > 0)
            return selectableIndices[random.NextInt(selectableIndices.Length)];

        return random.NextInt(operatorCount);
    }

    private int ChooseWeightedOperator(double sample)
    {
        var scaledSample = sample * totalWeight;
        for (var i = 0; i < cumulativeWeights.Length; i++)
        {
            if (scaledSample < cumulativeWeights[i])
                return i;
        }

        for (var i = cumulativeWeights.Length - 1; i >= 0; i--)
        {
            if (i == 0 || cumulativeWeights[i] > cumulativeWeights[i - 1])
                return i;
        }

        throw new InvalidOperationException("Weighted dispatch has no selectable operator.");
    }

    private int[] GetSelectableIndices(Func<double, bool> isSelectable, int count)
    {
        var indices = new int[count];
        var resultIndex = 0;
        for (var i = 0; i < Weights.Length; i++)
        {
            if (isSelectable(Weights[i]))
                indices[resultIndex++] = i;
        }

        return indices;
    }
}
