using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public record SelectSecondParentCrossover<TCandidate>
    : SingleCandidateCrossover<TCandidate>
{
    public static readonly SelectSecondParentCrossover<TCandidate> Instance = new();

    public override TCandidate CrossParents(Parents<TCandidate> parents, IRandomNumberGenerator random) => SelectSecondParentCrossover.Cross(parents, random);
}

public static class SelectSecondParentCrossover
{
    public static SelectSecondParentCrossover<TCandidate> For<TCandidate, TSearchSpace>(IProblem<TCandidate, TSearchSpace> problem)
        where TSearchSpace : class, ISearchSpace<TCandidate> => SelectSecondParentCrossover<TCandidate>.Instance;

    public static TCandidate Cross<TCandidate>(Parents<TCandidate> parents, IRandomNumberGenerator random) => parents.Parent2;
}
