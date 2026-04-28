using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public sealed class WeightedBatchDispatch
{
  public ImmutableArray<double> Weights { get; }

  private readonly double totalWeight;
  private readonly double[] cumulativeWeights;

  public WeightedBatchDispatch(ImmutableArray<double> weights)
  {
    if (weights.Length == 0) {
      throw new ArgumentException("At least one weight must be provided.", nameof(weights));
    }

    if (weights.Any(weight => weight < 0)) {
      throw new ArgumentException("Weights must be non-negative.", nameof(weights));
    }

    if (weights.All(weight => weight <= 0)) {
      throw new ArgumentException("At least one weight must be greater than zero.", nameof(weights));
    }

    Weights = weights;
    cumulativeWeights = new double[weights.Length];
    for (var i = 0; i < weights.Length; i++) {
      totalWeight += weights[i];
      cumulativeWeights[i] = totalWeight;
    }
  }

  public IReadOnlyList<TOutput> Dispatch<TInput, TOutput, TOperator>(
    IReadOnlyList<TInput> inputs,
    IReadOnlyList<TOperator> operators,
    IRandomNumberGenerator random,
    Func<TOperator, IReadOnlyList<TInput>, IReadOnlyList<TOutput>> invokeBatch)
  {
    if (operators.Count != Weights.Length) {
      throw new ArgumentException("Weights must have the same length as operators.", nameof(operators));
    }

    if (inputs.Count == 0) {
      return [];
    }

    var operatorAssignments = new int[inputs.Count];
    var operatorCounts = new int[operators.Count];
    var randoms = random.NextDoubles(inputs.Count);
    for (var i = 0; i < inputs.Count; i++) {
      var operatorIndex = ChooseOperator(randoms[i]);
      operatorAssignments[i] = operatorIndex;
      operatorCounts[operatorIndex]++;
    }

    var inputBatches = new List<TInput>[operators.Count];
    var indexBatches = new List<int>[operators.Count];
    for (var i = 0; i < operators.Count; i++) {
      inputBatches[i] = new List<TInput>(operatorCounts[i]);
      indexBatches[i] = new List<int>(operatorCounts[i]);
    }

    for (var i = 0; i < inputs.Count; i++) {
      var operatorIndex = operatorAssignments[i];
      inputBatches[operatorIndex].Add(inputs[i]);
      indexBatches[operatorIndex].Add(i);
    }

    var results = new TOutput[inputs.Count];
    for (var i = 0; i < operators.Count; i++) {
      if (inputBatches[i].Count == 0) {
        continue;
      }

      var batchResults = invokeBatch(operators[i], inputBatches[i]);
      if (batchResults.Count != inputBatches[i].Count) {
        throw new InvalidOperationException("A weighted batch operator returned a result count that does not match its input count.");
      }

      for (var j = 0; j < batchResults.Count; j++) {
        results[indexBatches[i][j]] = batchResults[j];
      }
    }

    return results;
  }

  private int ChooseOperator(double sample)
  {
    var scaledSample = sample * totalWeight;
    var operatorIndex = Array.FindIndex(cumulativeWeights, weight => scaledSample < weight);
    return operatorIndex >= 0 ? operatorIndex : cumulativeWeights.Length - 1;
  }
}

