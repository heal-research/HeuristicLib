using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Mutators;

public record NoChangeMutator<TCandidate> : SingleSolutionMutator<TCandidate>
{
    public static readonly NoChangeMutator<TCandidate> Instance = new();

    public override TCandidate Mutate(TCandidate parent, IRandomNumberGenerator random) => NoChangeMutator.Mutate(parent, random);
}

public static class NoChangeMutator
{
    public static TCandidate Mutate<TCandidate>(TCandidate parent, IRandomNumberGenerator random) => parent;
}
