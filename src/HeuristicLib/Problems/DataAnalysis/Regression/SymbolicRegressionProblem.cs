using HEAL.HeuristicLib.Encodings.SymbolicExpression;
using HEAL.HeuristicLib.Encodings.SymbolicExpression.Grammars;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public class SymbolicRegressionProblem(RegressionProblemData data, ICollection<IRegressionEvaluator> objective, IComparer<ObjectiveVector> a, SymbolicExpressionTreeEncoding encoding, ISymbolicDataAnalysisExpressionTreeInterpreter interpreter) :
  RegressionProblem<RegressionProblemData, SymbolicExpressionTree, SymbolicExpressionTreeEncoding>(data, objective, a, encoding) {
  protected override IRegressionModel Decode(SymbolicExpressionTree solution) => new SymbolicRegressionModel(solution, interpreter);
  public ISymbolicDataAnalysisExpressionTreeInterpreter Interpreter { get { return interpreter; } }

  public SymbolicRegressionProblem(RegressionProblemData data, ICollection<IRegressionEvaluator> objective) :
    this(data, objective,
      objective.Count == 1 ? new SingleObjectiveComparer(objective.Single().Direction) : new LexicographicComparer(objective.Select(x => x.Direction).ToArray()),
      new SymbolicExpressionTreeEncoding(new SimpleSymbolicExpressionGrammar()),
      new SymbolicDataAnalysisExpressionTreeInterpreter()) { }
}
