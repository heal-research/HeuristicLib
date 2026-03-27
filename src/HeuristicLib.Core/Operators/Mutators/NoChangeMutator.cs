using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Mutators;

public record NoChangeMutator<TGenotype> : SingleSolutionMutator<TGenotype>
{
  public static readonly NoChangeMutator<TGenotype> Instance = new();

  public override TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random)
  {
    return parent;
  }
}
