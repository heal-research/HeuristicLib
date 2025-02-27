namespace HEAL.HeuristicLib.Operators;

public interface ICrossover<TGenotype> 
{
  TGenotype Crossover(TGenotype parent1, TGenotype parent2);
}

public interface ICrossoverTemplate<out TCreator, TGenotype, in TParams> 
  : IOperatorTemplate<TCreator, TParams>
  where TCreator : ICreator<TGenotype> 
  where TParams : CreatorParameters {
}

public record CrossoverParameters();

public abstract class CrossoverBase<TGenotype> : ICrossover<TGenotype> {
  public abstract TGenotype Crossover(TGenotype parent1, TGenotype parent2);
}

public abstract class CrossoverTemplateBase<TCreator, TGenotype, TParams> 
  : ICrossoverTemplate<TCreator, TGenotype, TParams> 
  where TCreator : ICreator<TGenotype> 
  where TParams : CreatorParameters {
  public abstract TCreator Parameterize(TParams parameters);
}
