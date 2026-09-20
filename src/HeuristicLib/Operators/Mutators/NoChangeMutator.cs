using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public record NoChangeMutator<TCandidate> : SingleCandidateMutator<TCandidate>
{
    public static readonly NoChangeMutator<TCandidate> Instance = new();

    public override TCandidate MutateCandidate(TCandidate parent, IRandomNumberGenerator random) => NoChangeMutator.Mutate(parent, random);
}

public static class NoChangeMutator
{
    public static NoChangeMutator<TCandidate> For<TCandidate>(IProblem<TCandidate, ISearchSpace<TCandidate>> problem) => NoChangeMutator<TCandidate>.Instance;

    public static TCandidate Mutate<TCandidate>(TCandidate parent, IRandomNumberGenerator random) => parent;
}
