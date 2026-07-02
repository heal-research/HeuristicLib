// namespace HEAL.HeuristicLib.SearchSpaces;

// public interface IRecordSearchSpace<TCandidate, TSearchSpace1, TSearchSpace2> : IEncoding<TCandidate, IRecordSearchSpace<TCandidate, TSearchSpace1, TSearchSpace2>> {
//   TSearchSpace1 Encoding1 { get; }
//   TSearchSpace2 Encoding2 { get; }
// }

// public interface IRecordGenotypeBase<out TSelf, T1, T2> where TSelf : IRecordGenotypeBase<TSelf, T1, T2> {
//   static abstract TSelf Construct(T1 item1, T2 item2);
//   void Deconstruct(out T1 item1, out T2 item2);
// }
// 
// public class RecordCrossover<TCandidate, TSearchSpace, TGenotype1, TSearchSpace1, TGenotype2, TSearchSpace2> : CrossoverBase<TCandidate, TSearchSpace>
//   where TSearchSpace : IRecordSearchSpace<TCandidate, TSearchSpace1, TSearchSpace2>
//   where TCandidate : IRecordGenotypeBase<TCandidate, TGenotype1, TGenotype2>
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
//   public override TCandidate Cross<TContext>(TCandidate parent1, TCandidate parent2, TContext context)
//   //where TContext : ISearchSpaceContext<TSearchSpace1>, IRandomContext
//   {
//     var (parent1Chromosome1, parent1Chromosome2) = parent1;
//     var (parent2Chromosome1, parent2Chromosome2) = parent2;
//     var child1 = crossover1.Cross(parent1Chromosome1, parent2Chromosome1, context.SearchSpace.SearchSpace1);
//     var child2 = crossover2.Cross(parent1Chromosome2, parent2Chromosome2, context.SearchSpace.SearchSpace2);
//     return TCandidate.Construct(child1, child2);
//   }
// }



