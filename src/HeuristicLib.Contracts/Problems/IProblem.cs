using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems;

/// <summary>
/// What every problem has regardless of what it is searched over.
/// </summary>
/// <remarks>
/// The objective says which direction is better, which does not depend on the candidate type. Keeping it reachable
/// without the type parameters is what lets anything holding a problem rank results by it.
/// </remarks>
public interface IProblem
{
    ObjectiveDirections Objective { get; }
}

public interface IProblem<TCandidate, out TSearchSpace> : IProblem
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    TSearchSpace SearchSpace { get; }

    //ObjectiveVector Evaluate(TCandidate candidate, IRandomNumberGenerator random);
    IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random);
}
