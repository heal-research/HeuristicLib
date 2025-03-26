namespace HEAL.HeuristicLib.Operators;

public interface ICrossoverOperator<TGenotype> : IExecutableOperator {
  TGenotype Cross(TGenotype parent1, TGenotype parent2);
}

public static class CrossoverOperator {
  public static CrossoverOperator<TGenotype> Create<TGenotype>(Func<TGenotype, TGenotype, TGenotype> crossover) => new CrossoverOperator<TGenotype>(crossover);
}

public sealed class CrossoverOperator<TGenotype> : ICrossoverOperator<TGenotype> {
  private readonly Func<TGenotype, TGenotype, TGenotype> crossover;
  internal CrossoverOperator(Func<TGenotype, TGenotype, TGenotype> crossover) {
    this.crossover = crossover;
  }
  public TGenotype Cross(TGenotype parent1, TGenotype parent2) => crossover(parent1, parent2);
}

public abstract class CrossoverOperatorBase<TGenotype> : ICrossoverOperator<TGenotype> {
  public abstract TGenotype Cross(TGenotype parent1, TGenotype parent2); 
}
