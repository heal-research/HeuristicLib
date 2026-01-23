using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Clustering;

public interface IClusteringEvaluator
{
  ObjectiveDirection Direction { get; }
  double Evaluate<TProblemData>(TProblemData problemData, DataAnalysisProblemData.PartitionType predictedValues, IEnumerable<int> predictions) where TProblemData : ClusteringProblemData;
}
