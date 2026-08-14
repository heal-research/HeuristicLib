using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public record SelectFirstParentCrossover<TCandidate>
    : SingleCandidateCrossover<TCandidate>
{
    public static readonly SelectFirstParentCrossover<TCandidate> Instance = new();

    public override TCandidate CrossParents(Parents<TCandidate> parents, IRandomNumberGenerator random) => SelectFirstParentCrossover.Cross(parents, random);
}

public static class SelectFirstParentCrossover
{
    public static SelectFirstParentCrossover<TCandidate> For<TCandidate, TSearchSpace>(IProblem<TCandidate, TSearchSpace> problem)
        where TSearchSpace : class, ISearchSpace<TCandidate> => SelectFirstParentCrossover<TCandidate>.Instance;

    public static TCandidate Cross<TCandidate>(Parents<TCandidate> parents, IRandomNumberGenerator random) => parents.Parent1;
}
