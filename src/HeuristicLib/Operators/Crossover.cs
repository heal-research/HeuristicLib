using HEAL.HeuristicLib.Encodings;

namespace HEAL.HeuristicLib.Operators;

public interface ICrossover<TGenotype> 
{
  TGenotype Crossover(TGenotype parent1, TGenotype parent2);
}

public abstract class CrossoverBase<TGenotype> : ICrossover<TGenotype>
{
  public abstract TGenotype Crossover(TGenotype parent1, TGenotype parent2);
}



public interface IOperatorFactory<out TOperator, in TEncoding>
{
  TOperator Create(TEncoding encoding);
}
