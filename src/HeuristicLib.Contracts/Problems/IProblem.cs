using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems;

public interface IProblem<in TCandidate, out TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TCandidate>
{
    TSearchSpace SearchSpace { get; }
    ObjectiveDirections Objective { get; }

    //ObjectiveVector Evaluate(TCandidate candidate, IRandomNumberGenerator random);
    IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random);
}
