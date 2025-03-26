using HEAL.HeuristicLib.Algorithms;

namespace HEAL.HeuristicLib.Operators;

public interface IMutatorOperator<TGenotype> : IExecutableOperator {
  TGenotype Mutate(TGenotype parent);
}

public static class MutatorOperator {
  public static MutatorOperator<TGenotype> Create<TGenotype>(Func<TGenotype, TGenotype> mutator) => new MutatorOperator<TGenotype>(mutator);
}

public sealed class MutatorOperator<TGenotype> : IMutatorOperator<TGenotype> {
  private readonly Func<TGenotype, TGenotype> mutator;
  internal MutatorOperator(Func<TGenotype, TGenotype> mutator) {
    this.mutator = mutator;
  }
  public TGenotype Mutate(TGenotype parent) => mutator(parent);
}

public abstract class MutatorOperatorBase<TGenotype> : IMutatorOperator<TGenotype> {
  public abstract TGenotype Mutate(TGenotype parent);
}
