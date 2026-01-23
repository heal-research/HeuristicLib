using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Clustering;

public class ClusteringProblem<TProblemData, TSolution, TSearchSpace>(TProblemData problemData, ICollection<IClusteringEvaluator> objective, IComparer<ObjectiveVector> a, TSearchSpace encoding)
  : DataAnalysisProblem<TProblemData, TSolution, TSearchSpace>(problemData, new Objective(objective.Select(x => x.Direction).ToArray(), a), encoding)
  where TProblemData : ClusteringProblemData
  where TSearchSpace : class, ISearchSpace<TSolution>
  where TSolution : IClusteringModel {
  public List<IClusteringEvaluator> Evaluators { get; set; } = objective.ToList();

  public override ObjectiveVector Evaluate(TSolution solution) {
    var predictions = solution.GetClusterValues(ProblemData.Dataset, ProblemData.Partitions[DataAnalysisProblemData.PartitionType.Training].Enumerate());
    if (Evaluators.Count == 1)
      return new ObjectiveVector(Evaluators[0].Evaluate(ProblemData, DataAnalysisProblemData.PartitionType.Training, predictions));

    if (predictions is not ICollection<int> materialPredictions)
      materialPredictions = predictions.ToArray();
    return new ObjectiveVector(Evaluators.Select(x => x.Evaluate(ProblemData, DataAnalysisProblemData.PartitionType.Training, materialPredictions)).ToArray());
  }
}
