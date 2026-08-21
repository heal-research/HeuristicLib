using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Assigns each input to a weighted operator, invokes each selected operator with one batch, and restores result order.
/// </summary>
internal sealed class WeightedBatchDispatcher
{
    private readonly WeightedIndexSampler sampler;

    public WeightedBatchDispatcher(int operatorCount, IReadOnlyList<double>? weights = null)
    {
        sampler = new WeightedIndexSampler(operatorCount, weights);
    }

    /// <summary>Dispatches a requested number of outputs without materializing placeholder inputs.</summary>
    public IReadOnlyList<TOutput> Dispatch<TOutput, TOperator, TState>(int count, IReadOnlyList<TOperator> operators, IRandomNumberGenerator random, TState state, Func<TOperator, int, TState, IReadOnlyList<TOutput>> invokeBatch)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ValidateOperators(operators);

        if (count == 0)
            return [];

        var operatorAssignments = new int[count];
        var operatorCounts = new int[operators.Count];
        var operatorStarts = new int[operators.Count + 1];
        for (var i = 0; i < count; i++)
        {
            var operatorIndex = sampler.Sample(random);
            operatorAssignments[i] = operatorIndex;
            operatorCounts[operatorIndex]++;
        }

        for (var i = 0; i < operators.Count; i++)
            operatorStarts[i + 1] = operatorStarts[i] + operatorCounts[i];

        operatorStarts.AsSpan(0, operators.Count).CopyTo(operatorCounts);
        var groupedInputIndices = new int[count];
        for (var i = 0; i < count; i++)
        {
            var operatorIndex = operatorAssignments[i];
            groupedInputIndices[operatorCounts[operatorIndex]++] = i;
        }

        var results = new TOutput[count];
        for (var i = 0; i < operators.Count; i++)
        {
            var batchStart = operatorStarts[i];
            var batchCount = operatorStarts[i + 1] - batchStart;
            if (batchCount == 0)
                continue;

            var batchResults = invokeBatch(operators[i], batchCount, state);
            if (batchResults.Count != batchCount)
                throw new InvalidOperationException("A weighted batch operator returned a result count that does not match its requested count.");

            for (var j = 0; j < batchResults.Count; j++)
                results[groupedInputIndices[batchStart + j]] = batchResults[j];
        }

        return results;
    }

    public IReadOnlyList<TOutput> Dispatch<TInput, TOutput, TOperator>(IReadOnlyList<TInput> inputs, IReadOnlyList<TOperator> operators, IRandomNumberGenerator random, Func<TOperator, IReadOnlyList<TInput>, IReadOnlyList<TOutput>> invokeBatch) =>
        Dispatch(inputs, operators, random, invokeBatch, static (@operator, batch, invoke) => invoke(@operator, batch));

    public IReadOnlyList<TOutput> Dispatch<TInput, TOutput, TOperator, TState>(IReadOnlyList<TInput> inputs, IReadOnlyList<TOperator> operators, IRandomNumberGenerator random, TState state, Func<TOperator, IReadOnlyList<TInput>, TState, IReadOnlyList<TOutput>> invokeBatch)
    {
        ValidateOperators(operators);

        if (inputs.Count == 0)
            return [];

        var operatorAssignments = new int[inputs.Count];
        var operatorCounts = new int[operators.Count];
        var operatorStarts = new int[operators.Count + 1];
        for (var i = 0; i < inputs.Count; i++)
        {
            var operatorIndex = sampler.Sample(random);
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

    private void ValidateOperators<TOperator>(IReadOnlyList<TOperator> operators)
    {
        if (operators.Count == 0)
            throw new ArgumentException("At least one operator must be provided.", nameof(operators));
        if (operators.Count != sampler.Count)
            throw new ArgumentException("The sampler must have one entry for each operator.", nameof(operators));
    }
}
