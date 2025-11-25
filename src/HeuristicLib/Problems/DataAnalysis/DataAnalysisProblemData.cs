using HEAL.HeuristicLib.Problems.Dynamic;

namespace HEAL.HeuristicLib.Problems.DataAnalysis;

public abstract class DataAnalysisProblemData : IProblemData {
  public Dataset Dataset { get; set; }
  public List<string> InputVariables { get; }
  public Dictionary<PartitionType, Range> Partitions { get; }

  protected DataAnalysisProblemData(Dataset dataset, IEnumerable<string> inputs) {
    Dataset = dataset;
    InputVariables = inputs.Where(variable => dataset.VariableHasType<double>(variable) || dataset.VariableHasType<string>(variable)).ToList();
    Partitions = new Dictionary<PartitionType, Range> {
      [PartitionType.Training] = new(0, dataset.Rows / 2),
      [PartitionType.Test] = new(dataset.Rows / 2, dataset.Rows),
      [PartitionType.Validation] = new(0, 0),
      [PartitionType.All] = new(0, dataset.Rows)
    };
  }

  public enum PartitionType {
    Training,
    Test,
    Validation,
    All
  }
}
