namespace HEAL.HeuristicLib.Encodings;


// public interface IRecordSearchSpace<TRecordGenotype, TSearchSpace1, TSearchSpace2> : ISearchSpace<TRecordGenotype, IRecordSearchSpace<TRecordGenotype, TSearchSpace1, TSearchSpace2>> {
//   TSearchSpace1 SearchSpace1 { get; }
//   TSearchSpace2 SearchSpace2 { get; }
// }
//
// public interface IRecordGenotypeBase<out TSelf, T1, T2> where TSelf : IRecordGenotypeBase<TSelf, T1, T2> {
//   static abstract TSelf Construct(T1 item1, T2 item2);
//   void Deconstruct(out T1 item1, out T2 item2);
// }
//
// public class RecordCrossover<TGenotype, TSearchSpace, TGenotype1, TSearchSpace1, TGenotype2, TSearchSpace2> : CrossoverBase<TGenotype, TSearchSpace>
//   where TSearchSpace : IRecordSearchSpace<TGenotype, TSearchSpace1, TSearchSpace2>
//   where TGenotype : IRecordGenotypeBase<TGenotype, TGenotype1, TGenotype2>
//   where TSearchSpace1 : ISearchSpace<TGenotype1, TSearchSpace1>
//   where TSearchSpace2 : ISearchSpace<TGenotype2, TSearchSpace2> 
// {
//   private readonly ICrossover<TGenotype1, TSearchSpace1> crossover1;
//   private readonly ICrossover<TGenotype2, TSearchSpace2> crossover2;
//
//   public RecordCrossover(ICrossover<TGenotype1, TSearchSpace1> crossover1, ICrossover<TGenotype2, TSearchSpace2> crossover2) {
//     this.crossover1 = crossover1;
//     this.crossover2 = crossover2;
//   }
//
//   public override TGenotype Cross<TContext>(TGenotype parent1, TGenotype parent2, TContext context)
//   //where TContext : ISearchSpaceContext<TSearchSpace1>, IRandomContext
//   {
//     var (parent1Chromosome1, parent1Chromosome2) = parent1;
//     var (parent2Chromosome1, parent2Chromosome2) = parent2;
//     var child1 = crossover1.Cross(parent1Chromosome1, parent2Chromosome1, context.SearchSpace.SearchSpace1);
//     var child2 = crossover2.Cross(parent1Chromosome2, parent2Chromosome2, context.SearchSpace.SearchSpace2);
//     return TGenotype.Construct(child1, child2);
//   }
// }
