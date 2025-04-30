using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public abstract record class Crossover<TGenotype, TSearchSpace> : Operator<ICrossoverInstance<TGenotype, TSearchSpace>>
  where TSearchSpace : ISearchSpace<TGenotype>
{
}

public interface ICrossoverInstance<TGenotype, in TSearchSpace>
  where TSearchSpace : ISearchSpace<TGenotype>
{
  TGenotype Cross(TGenotype parent1, TGenotype parent2, TSearchSpace searchSpace, IRandomNumberGenerator random);
}

public abstract class CrossoverInstance<TGenotype, TSearchSpace, TCrossover> : OperatorInstance<TCrossover>, ICrossoverInstance<TGenotype, TSearchSpace>
  where TSearchSpace : ISearchSpace<TGenotype>
{
  protected CrossoverInstance(TCrossover parameters) : base(parameters) { }
  public abstract TGenotype Cross(TGenotype parent1, TGenotype parent2, TSearchSpace searchSpace, IRandomNumberGenerator random);
}
//
// public static class Crossover {
//   public static CustomCrossover<TGenotype, TSearchSpace> Create<TGenotype, TSearchSpace>(Func<TGenotype, TGenotype, TSearchSpace, IRandomNumberGenerator, TGenotype> crossover)
//     where TSearchSpace : ISearchSpace<TGenotype>
//   {
//     return new CustomCrossover<TGenotype, TSearchSpace>(crossover);
//   }
// }
//
// public sealed class CustomCrossover<TGenotype, TSearchSpace> 
//   : ICrossover<TGenotype, TSearchSpace>
//   where TSearchSpace : ISearchSpace<TGenotype>
// {
//   private readonly Func<TGenotype, TGenotype, TSearchSpace, IRandomNumberGenerator, TGenotype> crossover;
//   internal CustomCrossover(Func<TGenotype, TGenotype, TSearchSpace, IRandomNumberGenerator, TGenotype> crossover) {
//     this.crossover = crossover;
//   }
//   public TGenotype Cross(TGenotype parent1, TGenotype parent2, TSearchSpace searchSpace, IRandomNumberGenerator random) => crossover(parent1, parent2, searchSpace, random);
// }
//
// public abstract class CrossoverBase<TGenotype, TSearchSpace> 
//   : ICrossover<TGenotype, TSearchSpace>
//   where TSearchSpace : ISearchSpace<TGenotype> 
// {
//   public abstract TGenotype Cross(TGenotype parent1, TGenotype parent2, TSearchSpace searchSpace, IRandomNumberGenerator random); 
// }
