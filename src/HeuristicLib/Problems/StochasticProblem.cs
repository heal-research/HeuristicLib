using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems;

public abstract class StochasticProblem<TSolution, TEncoding>(Objective objective, TEncoding searchSpace, int? randomSeed = null) : Problem<TSolution, TEncoding>(objective, searchSpace), IStochasticProblem<TSolution, TEncoding>
  where TEncoding : class, IEncoding<TSolution> {
  public IRandomNumberGenerator ProblemRandom { get; } = new SystemRandomNumberGenerator(randomSeed);
  public abstract ObjectiveVector Evaluate(TSolution solution, IRandomNumberGenerator problemRandom);
  public sealed override ObjectiveVector Evaluate(TSolution solution) => throw new NotSupportedException($"Stochastic problems can not evaluate without random generator"); // this happens when the wrong evaluator is chosen
}

public interface IStochasticProblem<in TSolution, out TEncoding> : IProblem<TSolution, TEncoding>
  where TEncoding : class, IEncoding<TSolution> {
  IRandomNumberGenerator ProblemRandom { get; }
  ObjectiveVector Evaluate(TSolution solution, IRandomNumberGenerator problemRandom);
}

public abstract class StochasticProblem<TSolution>(Objective objective, int? randomSeed = null) : StochasticProblem<TSolution, IEncoding<TSolution>>(objective, AnyEncoding<TSolution>.Instance, randomSeed);

public interface IStatefulProblem<in TGenotype, out TEncoding, out TProblemData> : IProblem<TGenotype, TEncoding>
  where TProblemData : IProblemData
  where TEncoding : class, IEncoding<TGenotype> {
  TProblemData CurrentState { get; }

  void UpdateState();
}

public abstract class StatefulProblem<TSolution, TEncoding, TProblemData>(
  Objective objective,
  TEncoding searchSpace,
  TProblemData initialState,
  int? randomSeed = null) : StochasticProblem<TSolution, TEncoding>(objective, searchSpace, randomSeed),
                            IStatefulProblem<TSolution, TEncoding, TProblemData>
  where TEncoding : class, IEncoding<TSolution>
  where TProblemData : IProblemData {
  public TProblemData CurrentState { get; protected set; } = initialState;
  public abstract void UpdateState();
}
