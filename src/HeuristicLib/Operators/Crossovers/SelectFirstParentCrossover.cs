using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public record SelectFirstParentCrossover<TCandidate>
    : SingleCandidateCrossover<TCandidate>
{
    public static readonly SelectFirstParentCrossover<TCandidate> Instance = new();

    public override TCandidate CrossParents(Parents<TCandidate> parents, IRandomNumberGenerator random) => SelectFirstParentCrossover.Cross(parents, random);
}

public static class SelectFirstParentCrossover
{
    public static SelectFirstParentCrossover<TCandidate> For<TCandidate>(IProblem<TCandidate, ISearchSpace<TCandidate>> problem) => SelectFirstParentCrossover<TCandidate>.Instance;

    public static TCandidate Cross<TCandidate>(Parents<TCandidate> parents, IRandomNumberGenerator random) => parents.Parent1;
}
