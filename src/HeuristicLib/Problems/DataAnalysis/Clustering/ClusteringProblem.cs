using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Clustering;

public class ClusteringProblem<TProblemData, TSolution, TEncoding>(TProblemData problemData, ICollection<IClusteringEvaluator> objective, IComparer<ObjectiveVector> a, TEncoding encoding)
  : DataAnalysisProblem<TProblemData, TSolution, TEncoding>(problemData, new Objective(objective.Select(x => x.Direction).ToArray(), a), encoding)
  where TProblemData : ClusteringProblemData
  where TEncoding : class, IEncoding<TSolution>
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
