namespace HEAL.HeuristicLib.Operators;

public interface ICrossover<TGenotype> : IOperator
{
  TGenotype Crossover(TGenotype parent1, TGenotype parent2);
}

public interface ICrossoverTemplate<out TCrossover, TGenotype, in TParams> 
  : IOperatorTemplate<TCrossover, TParams>
  where TCrossover : ICrossover<TGenotype> 
  where TParams : CrossoverParameters {
}

public record CrossoverParameters();

public abstract class CrossoverBase<TGenotype> : ICrossover<TGenotype> {
  public abstract TGenotype Crossover(TGenotype parent1, TGenotype parent2);
}

public abstract class CrossoverTemplateBase<TCrossover, TGenotype, TParams> 
  : ICrossoverTemplate<TCrossover, TGenotype, TParams> 
  where TCrossover : ICrossover<TGenotype> 
  where TParams : CrossoverParameters {
  public abstract TCrossover Parameterize(TParams parameters);
}
