using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems;

/// <summary>Describes the part of a problem that does not depend on its candidate or search space types.</summary>
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
