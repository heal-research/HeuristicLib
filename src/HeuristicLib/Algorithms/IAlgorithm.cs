using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms;

public interface IAlgorithm<TGenotype, in TEncoding, in TProblem, TAlgorithmResult>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding>
  where TAlgorithmResult : IAlgorithmResult<TGenotype> {
  TimeSpan TotalExecutionTime { get; }
  OperatorMetric EvaluationsMetric { get; }

  TAlgorithmResult Execute(TProblem problem, TEncoding? searchSpace = null, IRandomNumberGenerator? random = null);
}
