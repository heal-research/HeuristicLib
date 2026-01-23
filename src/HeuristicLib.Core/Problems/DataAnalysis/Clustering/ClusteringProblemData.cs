namespace HEAL.HeuristicLib.Problems.DataAnalysis.Clustering;

public class ClusteringProblemData(Dataset dataset, IEnumerable<string> allowedInputVariables, IDistance<int> distance) : DataAnalysisProblemData(dataset, allowedInputVariables)
{
  public IDistance<int> Distance { get; set; } = distance;
}
