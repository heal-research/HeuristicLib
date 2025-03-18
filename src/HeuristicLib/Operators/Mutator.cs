namespace HEAL.HeuristicLib.Operators;

public interface IMutator<TGenotype> : IOperator {
  TGenotype Mutate(TGenotype parent);
}

public static class Mutator {
  public static IMutator<TGenotype> Create<TGenotype>(Func<TGenotype, TGenotype> mutator) => new Mutator<TGenotype>(mutator);
}

public sealed class Mutator<TGenotype> : IMutator<TGenotype> {
  private readonly Func<TGenotype, TGenotype> mutator;
  internal Mutator(Func<TGenotype, TGenotype> mutator) {
    this.mutator = mutator;
  }
  public TGenotype Mutate(TGenotype parent) => mutator(parent);
}

public abstract class MutatorBase<TGenotype> : IMutator<TGenotype> {
  public abstract TGenotype Mutate(TGenotype parent);
}
