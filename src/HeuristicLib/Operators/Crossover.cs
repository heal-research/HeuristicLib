using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public interface ICrossover<TGenotype, in TEncodingParameter>
  : IOperator
  where TEncodingParameter : IEncodingParameter<TGenotype>
{
  TGenotype Cross(TGenotype parent1, TGenotype parent2, TEncodingParameter encoding, IRandomNumberGenerator random);
}

public static class Crossover {
  public static CustomCrossover<TGenotype, TEncodingParameter> Create<TGenotype, TEncodingParameter>(Func<TGenotype, TGenotype, TEncodingParameter, IRandomNumberGenerator, TGenotype> crossover)
    where TEncodingParameter : IEncodingParameter<TGenotype>
  {
    return new CustomCrossover<TGenotype, TEncodingParameter>(crossover);
  }
}

public sealed class CustomCrossover<TGenotype, TEncodingParameter> 
  : ICrossover<TGenotype, TEncodingParameter>
  where TEncodingParameter : IEncodingParameter<TGenotype>
{
  private readonly Func<TGenotype, TGenotype, TEncodingParameter, IRandomNumberGenerator, TGenotype> crossover;
  internal CustomCrossover(Func<TGenotype, TGenotype, TEncodingParameter, IRandomNumberGenerator, TGenotype> crossover) {
    this.crossover = crossover;
  }
  public TGenotype Cross(TGenotype parent1, TGenotype parent2, TEncodingParameter encoding, IRandomNumberGenerator random) => crossover(parent1, parent2, encoding, random);
}

public abstract class CrossoverBase<TGenotype, TEncodingParameter> 
  : ICrossover<TGenotype, TEncodingParameter>
  where TEncodingParameter : IEncodingParameter<TGenotype> 
{
  public abstract TGenotype Cross(TGenotype parent1, TGenotype parent2, TEncodingParameter encoding, IRandomNumberGenerator random); 
}
