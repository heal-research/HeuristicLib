using HEAL.HeuristicLib.Encodings.SymbolicExpression;
using HEAL.HeuristicLib.Encodings.SymbolicExpression.Grammars;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public class SymbolicRegressionProblem(
  RegressionProblemData data,
  ICollection<IRegressionEvaluator<SymbolicExpressionTree>> objective,
  IComparer<ObjectiveVector> a,
  SymbolicExpressionTreeEncoding encoding,
  ISymbolicDataAnalysisExpressionTreeInterpreter interpreter,
  int parameterOptimizationIterations = -1) :
  RegressionProblem<RegressionProblemData, SymbolicExpressionTree, SymbolicExpressionTreeEncoding>(data, objective, a, encoding) {
  protected override IRegressionModel Decode(SymbolicExpressionTree solution) => new SymbolicRegressionModel(solution, Interpreter);

  public override ObjectiveVector Evaluate(SymbolicExpressionTree solution, IRandomNumberGenerator random) {
    var rows = ProblemData.Partitions[DataAnalysisProblemData.PartitionType.Training].Enumerate();
    var targets = ProblemData.TargetVariableValues(DataAnalysisProblemData.PartitionType.Training);
    return Evaluate(solution, rows, targets);
  }

  protected ObjectiveVector Evaluate(SymbolicExpressionTree solution, IEnumerable<int> rows, IReadOnlyList<double> targets) {
    var predictions = solution
                      .PredictAndAdjustScaling(Interpreter, ProblemData.Dataset, rows, targets)
                      .LimitToRange(LowerPredictionBound, UpperPredictionBound)
                      .ToArray();
    if (parameterOptimizationIterations > 0)
      _ = SymbolicRegressionParameterOptimization.OptimizeParameters(Interpreter, solution, ProblemData, DataAnalysisProblemData.PartitionType.Training, parameterOptimizationIterations, true, LowerPredictionBound, UpperPredictionBound);
    return new ObjectiveVector(Evaluators.Select(x => x.Evaluate(solution, targets, predictions)));
  }

  public ISymbolicDataAnalysisExpressionTreeInterpreter Interpreter { get { return interpreter; } }

  public SymbolicRegressionProblem(RegressionProblemData data, params ICollection<IRegressionEvaluator<SymbolicExpressionTree>> objective) :
    this(data, objective,
      objective.Count == 1 ? new SingleObjectiveComparer(objective.Single().Direction) : new LexicographicComparer(objective.Select(x => x.Direction).ToArray()),
      new SymbolicExpressionTreeEncoding(new SimpleSymbolicExpressionGrammar()),
      new SymbolicDataAnalysisExpressionTreeInterpreter()) { }
}
