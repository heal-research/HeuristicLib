using System.Collections.Concurrent;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public class RegressionProblemData(Dataset dataset, string targetVariable, IEnumerable<string>? allowedInputVariables = null, Range? trainingRange = null)
  : DataAnalysisProblemData(dataset, allowedInputVariables ?? dataset.GetVariableNames().Except([targetVariable]), trainingRange) {
  public string TargetVariable { get; } = targetVariable;
  private readonly ConcurrentDictionary<PartitionType, double[]> cachedTargets = [];

  public RegressionProblemData(Dataset dataset) : this(dataset, dataset.GetVariableNames()[^1]) { }

  public IReadOnlyList<double> TargetVariableValues(PartitionType partition) {
    if (cachedTargets.TryGetValue(partition, out var res)) return res;
    return cachedTargets[partition] = Dataset.GetDoubleValues(TargetVariable, Partitions[partition].Enumerate()).ToArray();
  }
}
