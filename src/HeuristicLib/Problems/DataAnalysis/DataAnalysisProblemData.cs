using HEAL.HeuristicLib.Problems.Dynamic;

namespace HEAL.HeuristicLib.Problems.DataAnalysis;

public abstract class DataAnalysisProblemData : IProblemData {
  public Dataset Dataset { get; set; }
  public List<string> InputVariables { get; }
  public Dictionary<PartitionType, Range> Partitions { get; }

  protected DataAnalysisProblemData(Dataset dataset, IEnumerable<string> inputs, Range? trainingRange = null) {
    Dataset = dataset;
    var trainingRange1 = trainingRange ?? new Range(0, dataset.Rows / 2);
    var testRange = new Range(trainingRange1.End, dataset.Rows);
    InputVariables = inputs.Where(variable => dataset.VariableHasType<double>(variable) || dataset.VariableHasType<string>(variable)).ToList();
    Partitions = new Dictionary<PartitionType, Range> {
      [PartitionType.Training] = trainingRange1,
      [PartitionType.Test] = testRange,
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
