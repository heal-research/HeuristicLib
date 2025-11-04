using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Mutator;

public class NoChangeMutator<TGenotype> : BatchMutator<TGenotype> {
  public static NoChangeMutator<TGenotype> Instance { get; } = new();

  public override IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random) {
    return parent;
  }
}
