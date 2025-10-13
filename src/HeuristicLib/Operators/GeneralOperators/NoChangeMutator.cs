using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public class NoChangeMutator<TGenotype> : BatchMutator<TGenotype> {
  public override IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random) {
    return parent;
  }
}
