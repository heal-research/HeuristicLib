namespace HEAL.HeuristicLib.Problems.DataAnalysis.Classification;

public class ClassificationProblemData(Dataset dataset, IEnumerable<string> allowedInputVariables, string targetVariable)
  : DataAnalysisProblemData(dataset, allowedInputVariables) {
  public string TargetVariable { get; set; } = targetVariable;
  public IEnumerable<double> TargetVariableValues(PartitionType partition) => Dataset.GetDoubleValues(TargetVariable, Partitions[partition].Enumerate());
  public IEnumerable<double> TargetVariableValues() => Dataset.GetDoubleValues(TargetVariable);
}
