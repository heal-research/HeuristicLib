using HEAL.HeuristicLib.Encodings.SymbolicExpression;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public static class RegressionProblemDataExtensions {
  public static ObjectiveVector Evaluate(IRegressionModel solution, Dataset dataset, IEnumerable<int> rows, IReadOnlyList<IRegressionEvaluator> evaluators, double[] targets) {
    var predictions = solution.Predict(dataset, rows);
    if (evaluators.Count == 1)
      return new(evaluators[0].Evaluate(targets, predictions));

    if (predictions is not ICollection<double> materialPredictions)
      materialPredictions = predictions.ToArray();
    return new(evaluators.Select(x => x.Evaluate(targets, materialPredictions)));
  }

  public static ObjectiveVector Evaluate(this RegressionProblemData data, IRegressionModel solution, DataAnalysisProblemData.PartitionType type, IReadOnlyList<IRegressionEvaluator> evaluators) {
    var targets = data.TargetVariableValues(type).ToArray();
    var rows = data.Partitions[type].Enumerate();
    return Evaluate(solution, data.Dataset, rows, evaluators, targets);
  }
}

public abstract class RegressionProblem<TProblemData, TSolution, TEncoding>(TProblemData problemData, ICollection<IRegressionEvaluator> objective, IComparer<ObjectiveVector> a, TEncoding encoding)
  : DataAnalysisProblem<TProblemData, TSolution, TEncoding>(problemData, new(objective.Select(x => x.Direction).ToArray(), a), encoding)
  where TProblemData : RegressionProblemData
  where TEncoding : class, IEncoding<TSolution> {
  public IReadOnlyList<IRegressionEvaluator> Evaluators { get; set; } = objective.ToList();

  private readonly double[] trainingTargetCache = problemData.TargetVariableValues(DataAnalysisProblemData.PartitionType.Training).ToArray();
  private readonly int[] rowIndicesCache = problemData.Partitions[DataAnalysisProblemData.PartitionType.Training].Enumerate().ToArray(); //unsure if this is faster than using the enumerable directly

  public override ObjectiveVector Evaluate(TSolution solution) => RegressionProblemDataExtensions.Evaluate(Decode(solution), ProblemData.Dataset, rowIndicesCache, Evaluators, trainingTargetCache);

  protected abstract IRegressionModel Decode(TSolution solution);
}

public record SymbolicRegressionModel(SymbolicExpressionTree tree, ISymbolicDataAnalysisExpressionTreeInterpreter interpreter) : IRegressionModel {
  public virtual IEnumerable<double> Predict(Dataset data, IEnumerable<int> rows) => interpreter.GetSymbolicExpressionTreeValues(tree, data, rows);
}

public record BoundedSymbolicRegressionModel(SymbolicExpressionTree tree, ISymbolicDataAnalysisExpressionTreeInterpreter interpreter, double lowerPredictionBound, double upperPredictionBound) :
  SymbolicRegressionModel(tree, interpreter) {
  public virtual IEnumerable<double> Predict(Dataset data, IEnumerable<int> rows) {
    return interpreter.GetSymbolicExpressionTreeValues(tree, data, rows).Select(v => Math.Min(upperPredictionBound, Math.Max(lowerPredictionBound, v)));
  }
}

public class SymbolicRegressionProblem(RegressionProblemData data, ICollection<IRegressionEvaluator> objective, IComparer<ObjectiveVector> a, SymbolicExpressionTreeEncoding encoding, ISymbolicDataAnalysisExpressionTreeInterpreter interpreter) :
  RegressionProblem<RegressionProblemData, SymbolicExpressionTree, SymbolicExpressionTreeEncoding>(data, objective, a, encoding) {
  protected override IRegressionModel Decode(SymbolicExpressionTree solution) => new SymbolicRegressionModel(solution, interpreter);
}
