using HEAL.HeuristicLib.Algorithms;

namespace HEAL.HeuristicLib.Operators;

public interface IMutator<TGenotype> : IOperator {
  TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random);
}

public static class Mutator {
  public static IMutator<TGenotype> Create<TGenotype>(Func<TGenotype, IRandomNumberGenerator, TGenotype> mutator) => new Mutator<TGenotype>(mutator);
}

public sealed class Mutator<TGenotype> : IMutator<TGenotype> {
  private readonly Func<TGenotype, IRandomNumberGenerator, TGenotype> mutator;
  internal Mutator(Func<TGenotype, IRandomNumberGenerator, TGenotype> mutator) {
    this.mutator = mutator;
  }
  public TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random) => mutator(parent, random);
}

public abstract class MutatorBase<TGenotype> : IMutator<TGenotype> {
  public abstract TGenotype Mutate(TGenotype parent, IRandomNumberGenerator random);
}
