using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems;

public interface IProblem<in TGenotype, out TEncoding>
  where TEncoding : class, IEncoding<TGenotype> {
  TEncoding SearchSpace { get; }
  Objective Objective { get; }

  public ObjectiveVector Evaluate(TGenotype solution, IRandomNumberGenerator random);
}
