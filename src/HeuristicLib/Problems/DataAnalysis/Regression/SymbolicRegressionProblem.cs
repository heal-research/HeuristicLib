using HEAL.HeuristicLib.Encodings.SymbolicExpression;
using HEAL.HeuristicLib.Encodings.SymbolicExpression.Grammars;
using HEAL.HeuristicLib.Operators.SymbolicExpression.Formatters;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic;
using LanguageExt;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public class SymbolicRegressionProblem(RegressionProblemData data, ICollection<IRegressionEvaluator> objective, IComparer<ObjectiveVector> a, SymbolicExpressionTreeEncoding encoding, ISymbolicDataAnalysisExpressionTreeInterpreter interpreter) :
  RegressionProblem<RegressionProblemData, SymbolicExpressionTree, SymbolicExpressionTreeEncoding>(data, objective, a, encoding) {
  protected override IRegressionModel Decode(SymbolicExpressionTree solution) => new SymbolicRegressionModel(solution, interpreter);

  public override ObjectiveVector Evaluate(SymbolicExpressionTree solution) {
    var plot = new SymbolicExpressionTreeGraphvizFormatter().Format(solution);

    var rows = ProblemData.Partitions[DataAnalysisProblemData.PartitionType.Training].Enumerate();
    var targets = ProblemData.TargetVariableValues(DataAnalysisProblemData.PartitionType.Training);
    var predictions = LinearScaling.GetAndAdjustScaling(interpreter, solution, ProblemData.Dataset, rows, targets).LimitToRange(LowerPredictionBound, UpperPredictionBound).ToArray();
    SymbolicRegressionParameterOptimization.OptimizeParameters(interpreter, solution, ProblemData, DataAnalysisProblemData.PartitionType.Training, 5, true, LowerPredictionBound, UpperPredictionBound);
    return new ObjectiveVector(Evaluators.Select(x => x.Evaluate(targets, predictions)));
  }

  public ISymbolicDataAnalysisExpressionTreeInterpreter Interpreter { get { return interpreter; } }

  public SymbolicRegressionProblem(RegressionProblemData data, ICollection<IRegressionEvaluator> objective) :
    this(data, objective,
      objective.Count == 1 ? new SingleObjectiveComparer(objective.Single().Direction) : new LexicographicComparer(objective.Select(x => x.Direction).ToArray()),
      new SymbolicExpressionTreeEncoding(new SimpleSymbolicExpressionGrammar()),
      new SymbolicDataAnalysisExpressionTreeInterpreter()) { }
}
