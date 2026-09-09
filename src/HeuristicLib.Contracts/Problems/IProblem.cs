using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems;

public interface IProblem<TCandidate, out TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    TSearchSpace SearchSpace { get; }
    ObjectiveDirections Objective { get; }

    //ObjectiveVector Evaluate(TCandidate candidate, IRandomNumberGenerator random);
    IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random);
}
