using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public record NoChangeRefiner<TCandidate> : SingleCandidateRefiner<TCandidate>
{
    public static readonly NoChangeRefiner<TCandidate> Instance = new();

    public override TCandidate RefineCandidate(TCandidate candidate, IRandomNumberGenerator random) => NoChangeRefiner.Refine(candidate, random);
}

public static class NoChangeRefiner
{
    public static NoChangeRefiner<TCandidate> For<TCandidate>(IProblem<TCandidate, ISearchSpace<TCandidate>> problem) => NoChangeRefiner<TCandidate>.Instance;

    public static TCandidate Refine<TCandidate>(TCandidate candidate, IRandomNumberGenerator random) => candidate;
}
