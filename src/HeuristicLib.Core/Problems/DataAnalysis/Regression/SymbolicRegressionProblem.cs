using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic;
using HEAL.HeuristicLib.SearchSpaces.Trees;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Grammars;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public class SymbolicRegressionProblem :
  RegressionProblem<RegressionProblemData, SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace>
{

  public SymbolicRegressionProblem(RegressionProblemData data, params ICollection<IRegressionEvaluator<SymbolicExpressionTree>> objective) :
    this(data, objective, GetDefaultComparer(objective), new SymbolicExpressionTreeSearchSpace(new SimpleSymbolicExpressionGrammar()))
  {
  }

  public SymbolicRegressionProblem(RegressionProblemData data, SymbolicExpressionTreeSearchSpace encoding, params ICollection<IRegressionEvaluator<SymbolicExpressionTree>> objective) :
    this(data, objective, GetDefaultComparer(objective), encoding)
  {
  }

  public SymbolicRegressionProblem(RegressionProblemData data,
    ICollection<IRegressionEvaluator<SymbolicExpressionTree>> objective,
    IComparer<ObjectiveVector> a,
    SymbolicExpressionTreeSearchSpace encoding) : base(data, objective, a, encoding)
  {
  }
  public ISymbolicDataAnalysisExpressionTreeInterpreter Interpreter { get; init; } = new SymbolicDataAnalysisExpressionTreeInterpreter();
  public int ParameterOptimizationIterations { get; init; } = -1;

  public override IEnumerable<double> PredictAndTrain(SymbolicExpressionTree solution, IReadOnlyList<int> rows, IReadOnlyList<double> targets)
  {
    if (ParameterOptimizationIterations > 0) {
      _ = SymbolicRegressionParameterOptimization.OptimizeParameters(
      Interpreter,
      solution,
      ProblemData,
      rows,
      ParameterOptimizationIterations,
      true,
      LowerPredictionBound,
      UpperPredictionBound);
    }

    return solution.PredictAndAdjustScaling(Interpreter, ProblemData.Dataset, rows, targets);
  }

  public static SymbolicExpressionTreeSearchSpace GetDefaultEncoding(IEnumerable<string> variableNames)
  {
    var grammar = new SimpleSymbolicExpressionGrammar();
    var root = grammar.AddLinearScaling();
    grammar.AddFullyConnectedSymbols(root, new Addition(), new Subtraction(), new Multiplication(), new Division(), new Number(), new SquareRoot(), new Logarithm(), new Variable { VariableNames = variableNames });

    return new SymbolicExpressionTreeSearchSpace(grammar);
  }

  private static IComparer<ObjectiveVector>
    GetDefaultComparer(ICollection<IRegressionEvaluator<SymbolicExpressionTree>> objective) => objective.Count == 1
    ? new SingleObjectiveComparer(objective.Single().Direction)
    : new LexicographicComparer(objective.Select(x => x.Direction).ToArray());
}
