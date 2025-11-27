using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Formatters;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Grammars;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public class SymbolicRegressionProblem :
  RegressionProblem<RegressionProblemData, SymbolicExpressionTree, SymbolicExpressionTreeEncoding> {
  public ISymbolicDataAnalysisExpressionTreeInterpreter Interpreter { get; init; } = new SymbolicDataAnalysisExpressionTreeInterpreter();
  public int ParameterOptimizationIterations { get; init; } = -1;
  protected override IRegressionModel Decode(SymbolicExpressionTree solution) => new SymbolicRegressionModel(solution, Interpreter);

  public override ObjectiveVector Evaluate(SymbolicExpressionTree solution) {
    var rows = ProblemData.Partitions[DataAnalysisProblemData.PartitionType.Training].Enumerate();
    var targets = ProblemData.TargetVariableValues(DataAnalysisProblemData.PartitionType.Training);
    return Evaluate(solution, rows, targets);
  }

  public ObjectiveVector Evaluate(SymbolicExpressionTree solution, IEnumerable<int> rows, IReadOnlyList<double> targets) {
    var debug = new SymbolicExpressionTreeGraphvizFormatter().Format(solution);

    if (ParameterOptimizationIterations > 0) {
      var materialRows = rows as IReadOnlyList<int> ?? rows.ToArray();
      rows = materialRows;
      _ = SymbolicRegressionParameterOptimization.OptimizeParameters(
        Interpreter,
        solution,
        ProblemData,
        materialRows,
        ParameterOptimizationIterations,
        true,
        LowerPredictionBound,
        UpperPredictionBound);
    }

    var predictions = solution
                      .PredictAndAdjustScaling(Interpreter, ProblemData.Dataset, rows, targets)
                      .LimitToRange(LowerPredictionBound, UpperPredictionBound)
                      .ToArray();
    return new ObjectiveVector(Evaluators.Select(x => x.Evaluate(solution, targets, predictions)));
  }

  public SymbolicRegressionProblem(RegressionProblemData data, params ICollection<IRegressionEvaluator<SymbolicExpressionTree>> objective) :
    this(data, objective,
      GetDefaultComparer(objective),
      new SymbolicExpressionTreeEncoding(new SimpleSymbolicExpressionGrammar())) { }

  public SymbolicRegressionProblem(RegressionProblemData data,
                                   ICollection<IRegressionEvaluator<SymbolicExpressionTree>> objective,
                                   IComparer<ObjectiveVector> a,
                                   SymbolicExpressionTreeEncoding encoding) : base(data, objective, a, encoding) { }

  private static IComparer<ObjectiveVector>
    GetDefaultComparer(ICollection<IRegressionEvaluator<SymbolicExpressionTree>> objective) => objective.Count == 1
    ? new SingleObjectiveComparer(objective.Single().Direction)
    : new LexicographicComparer(objective.Select(x => x.Direction).ToArray());
}
