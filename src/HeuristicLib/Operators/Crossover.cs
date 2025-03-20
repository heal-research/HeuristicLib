using HEAL.HeuristicLib.Algorithms;

namespace HEAL.HeuristicLib.Operators;

public interface ICrossover<TGenotype> : IOperator {
  TGenotype Cross(TGenotype parent1, TGenotype parent2, IRandomNumberGenerator random);
}

public static class Crossover {
  public static ICrossover<TGenotype> Create<TGenotype>(Func<TGenotype, TGenotype, IRandomNumberGenerator, TGenotype> crossover) => new Crossover<TGenotype>(crossover);
}

public sealed class Crossover<TGenotype> : ICrossover<TGenotype> {
  private readonly Func<TGenotype, TGenotype, IRandomNumberGenerator, TGenotype> crossover;
  internal Crossover(Func<TGenotype, TGenotype, IRandomNumberGenerator, TGenotype> crossover) {
    this.crossover = crossover;
  }
  public TGenotype Cross(TGenotype parent1, TGenotype parent2, IRandomNumberGenerator random) => crossover(parent1, parent2, random);
}

public abstract class CrossoverBase<TGenotype> : ICrossover<TGenotype> {
  public abstract TGenotype Cross(TGenotype parent1, TGenotype parent2, IRandomNumberGenerator random); 
}

// public interface IRecordEncoding<TRecordGenotype, TEncoding1, TEncoding2> : IEncoding<TRecordGenotype, IRecordEncoding<TRecordGenotype, TEncoding1, TEncoding2>> {
//   TEncoding1 Encoding1 { get; }
//   TEncoding2 Encoding2 { get; }
// }
//
// public interface IRecordGenotypeBase<out TSelf, T1, T2> where TSelf : IRecordGenotypeBase<TSelf, T1, T2> {
//   static abstract TSelf Construct(T1 item1, T2 item2);
//   void Deconstruct(out T1 item1, out T2 item2);
// }
//
// public class RecordCrossover<TGenotype, TEncoding, TGenotype1, TEncoding1, TGenotype2, TEncoding2> : CrossoverBase<TGenotype, TEncoding>
//   where TEncoding : IRecordEncoding<TGenotype, TEncoding1, TEncoding2>
//   where TGenotype : IRecordGenotypeBase<TGenotype, TGenotype1, TGenotype2>
//   where TEncoding1 : IEncoding<TGenotype1, TEncoding1>
//   where TEncoding2 : IEncoding<TGenotype2, TEncoding2> 
// {
//   private readonly ICrossover<TGenotype1, TEncoding1> crossover1;
//   private readonly ICrossover<TGenotype2, TEncoding2> crossover2;
//
//   public RecordCrossover(ICrossover<TGenotype1, TEncoding1> crossover1, ICrossover<TGenotype2, TEncoding2> crossover2) {
//     this.crossover1 = crossover1;
//     this.crossover2 = crossover2;
//   }
//
//   public override TGenotype Cross<TContext>(TGenotype parent1, TGenotype parent2, TContext context)
//   //where TContext : IEncodingContext<TEncoding1>, IRandomContext
//   {
//     var (parent1Chromosome1, parent1Chromosome2) = parent1;
//     var (parent2Chromosome1, parent2Chromosome2) = parent2;
//     var child1 = crossover1.Cross(parent1Chromosome1, parent2Chromosome1, context.Encoding.Encoding1);
//     var child2 = crossover2.Cross(parent1Chromosome2, parent2Chromosome2, context.Encoding.Encoding2);
//     return TGenotype.Construct(child1, child2);
//   }
// }
