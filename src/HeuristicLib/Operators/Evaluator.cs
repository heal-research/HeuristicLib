using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators;

public static class Evaluator {
  public static Evaluator<TGenotype> CreateEvaluator<TGenotype>(this IProblem<TGenotype, IEncoding<TGenotype>> problem) => new();
  public static StochasticEvaluator<TGenotype> CreateEvaluator<TGenotype>(this IStochasticProblem<TGenotype, IEncoding<TGenotype>> problem) => new();
}

public class Evaluator<TGenotype> : Evaluator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>;
