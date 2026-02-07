using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Mutators;

public class NoChangeMutator<TGenotype> : SingleSolutionStatelessMutator<TGenotype>
{
  public static NoChangeMutator<TGenotype> Instance { get; } = new();

  public override TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random)
  {
    return parent;
  }
}
