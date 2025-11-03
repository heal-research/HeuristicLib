using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Classification;

public class ClassificationProblem<TProblemData, TSolution, TEncoding>(TProblemData problemData, ICollection<IClassificationEvaluator> objective, IComparer<ObjectiveVector> a, TEncoding encoding)
  : DataAnalysisProblem<TProblemData, TSolution, TEncoding>(problemData, new Objective(objective.Select(x => x.Direction).ToArray(), a), encoding)
  where TProblemData : ClassificationProblemData
  where TEncoding : class, IEncoding<TSolution>
  where TSolution : IRegressionModel {
  public List<IClassificationEvaluator> Evaluators { get; set; } = objective.ToList();

  private double[]? trainingTargetCache;

  public override ObjectiveVector Evaluate(TSolution solution, IRandomNumberGenerator random) {
    trainingTargetCache ??= ProblemData.TargetVariableValues(DataAnalysisProblemData.PartitionType.Training).ToArray();
    var predictions = solution.Predict(ProblemData.Dataset, ProblemData.Partitions[DataAnalysisProblemData.PartitionType.Training].Enumerate());
    if (Evaluators.Count == 1)
      return new ObjectiveVector(Evaluators[0].Evaluate(trainingTargetCache, predictions));

    if (predictions is not ICollection<double> materialPredictions)
      materialPredictions = predictions.ToArray();
    return new ObjectiveVector(Evaluators.Select(x => x.Evaluate(trainingTargetCache, materialPredictions)).ToArray());
  }
}
