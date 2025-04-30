using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public abstract record class Mutator<TGenotype, TSearchSpace> : Operator<IMutatorInstance<TGenotype, TSearchSpace>>
  where TSearchSpace : ISearchSpace<TGenotype>
{
}

public interface IMutatorInstance<TGenotype, in TSearchSpace> 
  where TSearchSpace : ISearchSpace<TGenotype> 
{
  TGenotype Mutate(TGenotype parent, TSearchSpace searchSpace, IRandomNumberGenerator random);
}

public abstract class MutatorInstance<TGenotype, TSearchSpace, TMutator> : OperatorInstance<TMutator>, IMutatorInstance<TGenotype, TSearchSpace>
  where TSearchSpace : ISearchSpace<TGenotype>
{
  protected MutatorInstance(TMutator parameters) : base(parameters) { }
  public abstract TGenotype Mutate(TGenotype parent, TSearchSpace searchSpace, IRandomNumberGenerator random);
}
//
// public static class Mutator {
//   public static CustomMutator<TGenotype, TSearchSpace> Create<TGenotype, TSearchSpace>(Func<TGenotype, TSearchSpace, IRandomNumberGenerator, TGenotype> mutator) 
//     where TSearchSpace : ISearchSpace<TGenotype>
//   {
//     return new CustomMutator<TGenotype, TSearchSpace>(mutator);
//   }
// }
//
// public sealed class CustomMutator<TGenotype, TSearchSpace> 
//   : IMutator<TGenotype, TSearchSpace>
//   where TSearchSpace : ISearchSpace<TGenotype>
// {
//   private readonly Func<TGenotype, TSearchSpace, IRandomNumberGenerator, TGenotype> mutator;
//   internal CustomMutator(Func<TGenotype, TSearchSpace, IRandomNumberGenerator, TGenotype> mutator) {
//     this.mutator = mutator;
//   }
//   public TGenotype Mutate(TGenotype parent, TSearchSpace searchSpace, IRandomNumberGenerator random) => mutator(parent, searchSpace, random);
// }
//
// public abstract class MutatorBase<TGenotype, TSearchSpace>
//   : IMutator<TGenotype, TSearchSpace>
//   where TSearchSpace : ISearchSpace<TGenotype>
// {
//   public abstract TGenotype Mutate(TGenotype parent, TSearchSpace searchSpace, IRandomNumberGenerator random);
// }
