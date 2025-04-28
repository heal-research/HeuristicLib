using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public abstract record class Crossover<TGenotype, TEncoding> : Operator<ICrossoverInstance<TGenotype, TEncoding>>
  where TEncoding : IEncoding<TGenotype>
{
}

public interface ICrossoverInstance<TGenotype, in TEncoding>
  where TEncoding : IEncoding<TGenotype>
{
  TGenotype Cross(TGenotype parent1, TGenotype parent2, TEncoding encoding, IRandomNumberGenerator random);
}

public abstract class CrossoverInstance<TGenotype, TEncoding, TCrossover> : OperatorInstance<TCrossover>, ICrossoverInstance<TGenotype, TEncoding>
  where TEncoding : IEncoding<TGenotype>
{
  protected CrossoverInstance(TCrossover parameters) : base(parameters) { }
  public abstract TGenotype Cross(TGenotype parent1, TGenotype parent2, TEncoding encoding, IRandomNumberGenerator random);
}
//
// public static class Crossover {
//   public static CustomCrossover<TGenotype, TEncoding> Create<TGenotype, TEncoding>(Func<TGenotype, TGenotype, TEncoding, IRandomNumberGenerator, TGenotype> crossover)
//     where TEncoding : IEncoding<TGenotype>
//   {
//     return new CustomCrossover<TGenotype, TEncoding>(crossover);
//   }
// }
//
// public sealed class CustomCrossover<TGenotype, TEncoding> 
//   : ICrossover<TGenotype, TEncoding>
//   where TEncoding : IEncoding<TGenotype>
// {
//   private readonly Func<TGenotype, TGenotype, TEncoding, IRandomNumberGenerator, TGenotype> crossover;
//   internal CustomCrossover(Func<TGenotype, TGenotype, TEncoding, IRandomNumberGenerator, TGenotype> crossover) {
//     this.crossover = crossover;
//   }
//   public TGenotype Cross(TGenotype parent1, TGenotype parent2, TEncoding encoding, IRandomNumberGenerator random) => crossover(parent1, parent2, encoding, random);
// }
//
// public abstract class CrossoverBase<TGenotype, TEncoding> 
//   : ICrossover<TGenotype, TEncoding>
//   where TEncoding : IEncoding<TGenotype> 
// {
//   public abstract TGenotype Cross(TGenotype parent1, TGenotype parent2, TEncoding encoding, IRandomNumberGenerator random); 
// }
