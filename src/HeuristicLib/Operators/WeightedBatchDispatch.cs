using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public sealed class WeightedBatchDispatch
{
    public ImmutableArray<double> Weights { get; }

    public WeightedBatchDispatch(ImmutableArray<double> weights)
    {
        Weights = WeightSelection.Normalize(weights, weights.Length);
    }

    public IReadOnlyList<TOutput> Dispatch<TInput, TOutput, TOperator>(
      IReadOnlyList<TInput> inputs,
      IReadOnlyList<TOperator> operators,
      IRandomNumberGenerator random,
      Func<TOperator, IReadOnlyList<TInput>, IReadOnlyList<TOutput>> invokeBatch)
    {
        if (operators.Count != Weights.Length && !Weights.IsEmpty)
        {
            throw new ArgumentException("Weights must have the same length as operators.", nameof(operators));
        }

        if (inputs.Count == 0)
        {
            return [];
        }

        var operatorAssignments = new int[inputs.Count];
        var operatorCounts = new int[operators.Count];
        for (var i = 0; i < inputs.Count; i++)
        {
            var operatorIndex = WeightSelection.SelectIndex(random, operators.Count, Weights);
            operatorAssignments[i] = operatorIndex;
            operatorCounts[operatorIndex]++;
        }

        var inputBatches = new List<TInput>[operators.Count];
        var indexBatches = new List<int>[operators.Count];
        for (var i = 0; i < operators.Count; i++)
        {
            inputBatches[i] = new List<TInput>(operatorCounts[i]);
            indexBatches[i] = new List<int>(operatorCounts[i]);
        }

        for (var i = 0; i < inputs.Count; i++)
        {
            var operatorIndex = operatorAssignments[i];
            inputBatches[operatorIndex].Add(inputs[i]);
            indexBatches[operatorIndex].Add(i);
        }

        var results = new TOutput[inputs.Count];
        for (var i = 0; i < operators.Count; i++)
        {
            if (inputBatches[i].Count == 0)
            {
                continue;
            }

            var batchResults = invokeBatch(operators[i], inputBatches[i]);
            if (batchResults.Count != inputBatches[i].Count)
            {
                throw new InvalidOperationException("A weighted batch operator returned a result count that does not match its input count.");
            }

            for (var j = 0; j < batchResults.Count; j++)
            {
                results[indexBatches[i][j]] = batchResults[j];
            }
        }

        return results;
    }
}
