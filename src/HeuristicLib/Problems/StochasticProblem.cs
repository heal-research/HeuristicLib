using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Encodings.SymbolicExpression.Grammars;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems;

public abstract class StochasticProblem<TSolution, TEncoding>(Objective objective, TEncoding searchSpace, int? randomSeed = null) : Problem<TSolution, TEncoding>(objective, searchSpace), IStochasticProblem<TSolution, TEncoding>
  where TEncoding : class, IEncoding<TSolution> {
  public IRandomNumberGenerator ProblemRandom { get; } = new SystemRandomNumberGenerator(randomSeed);
  public abstract ObjectiveVector Evaluate(TSolution solution, IRandomNumberGenerator problemRandom);
  public sealed override ObjectiveVector Evaluate(TSolution solution) => throw new NotSupportedException($"Stochastic problems can not evaluate without random generator"); // this happens when the wrong evaluator is chosen
}

public abstract class StochasticProblem<TSolution>(Objective objective, int? randomSeed = null) : StochasticProblem<TSolution, IEncoding<TSolution>>(objective, AnyEncoding<TSolution>.Instance, randomSeed);
