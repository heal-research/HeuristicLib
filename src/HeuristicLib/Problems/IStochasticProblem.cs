using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems;

public interface IStochasticProblem<in TSolution, out TEncoding> : IProblem<TSolution, TEncoding>
  where TEncoding : class, IEncoding<TSolution> {
  IRandomNumberGenerator ProblemRandom { get; }
  ObjectiveVector Evaluate(TSolution solution, IRandomNumberGenerator problemRandom);
}
