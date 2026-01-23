using System.Collections.Concurrent;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public class RegressionProblemData(Dataset dataset, IEnumerable<string> allowedInputVariables, string targetVariable)
  : DataAnalysisProblemData(dataset, allowedInputVariables)
{
  public string TargetVariable { get; } = targetVariable;
  private readonly ConcurrentDictionary<PartitionType, double[]> cachedTargets = [];

  public RegressionProblemData(Dataset dataset)
    : this(dataset, dataset.GetVariableNames().SkipLast(1), dataset.GetVariableNames()[^1])
  {
  }

  public IReadOnlyList<double> TargetVariableValues(PartitionType partition)
  {
    if (cachedTargets.TryGetValue(partition, out var res)) {
      return res;
    }

    return cachedTargets[partition] = Dataset.GetDoubleValues(TargetVariable, Partitions[partition].Enumerate()).ToArray();
  }
}
