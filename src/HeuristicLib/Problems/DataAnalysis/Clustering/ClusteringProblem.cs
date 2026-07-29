using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Clustering;

public class ClusteringProblem<TProblemData, TCandidate, TSearchSpace>(TProblemData problemData, ICollection<IClusteringEvaluator> objective, IComparer<ObjectiveVector> a, TSearchSpace encoding)
  : DataAnalysisProblem<TProblemData, TCandidate, TSearchSpace>(problemData, new ObjectiveDirections(objective.Select(x => x.Direction).ToArray(), a), encoding)
  where TProblemData : ClusteringProblemData
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TCandidate : IClusteringModel
{
    public List<IClusteringEvaluator> Evaluators { get; set; } = objective.ToList();

    public override ObjectiveVector Evaluate(TCandidate candidate)
    {
        var predictions = candidate.GetClusterValues(ProblemData.Dataset, ProblemData.Partitions[DataAnalysisProblemData.PartitionType.Training].Enumerate());
        if (Evaluators.Count == 1)
        {
            return new ObjectiveVector(Evaluators[0].Evaluate(ProblemData, DataAnalysisProblemData.PartitionType.Training, predictions));
        }

        if (predictions is not ICollection<int> materialPredictions)
        {
            materialPredictions = predictions.ToArray();
        }

        return new ObjectiveVector(Evaluators.Select(x => x.Evaluate(ProblemData, DataAnalysisProblemData.PartitionType.Training, materialPredictions)).ToArray());
    }
}
