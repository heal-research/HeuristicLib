using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms;

public abstract class Algorithm<TGenotype, TEncoding, TProblem, TAlgorithmResult> : IAlgorithm<TGenotype, TEncoding, TProblem, TAlgorithmResult>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding>
  where TAlgorithmResult : IAlgorithmResult {
  public TimeSpan TotalExecutionTime { get; protected set; } = TimeSpan.Zero;
  public OperatorMetric EvaluationsMetric { get; protected set; } = OperatorMetric.Zero;

  public abstract TAlgorithmResult Execute(TProblem problem, TEncoding? searchSpace = null, IRandomNumberGenerator? random = null);
}
