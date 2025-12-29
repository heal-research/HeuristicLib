using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems;

public interface IProblem<in TGenotype, out TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype> {
  TSearchSpace SearchSpace { get; }
  Objective Objective { get; }

  public ObjectiveVector Evaluate(TGenotype solution, IRandomNumberGenerator random);
}
