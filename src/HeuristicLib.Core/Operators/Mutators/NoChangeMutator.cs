using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Mutators;

public record NoChangeMutator<TGenotype> : SingleSolutionMutator<TGenotype>
{
  public static readonly NoChangeMutator<TGenotype> Instance = new();

  public override TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random) => NoChangeMutator.Mutate(parent, random);
}

public static class NoChangeMutator
{
  public static TGenotype Mutate<TGenotype>(TGenotype parent, IRandomNumberGenerator random) => parent;
}
