using HEAL.HeuristicLib.Encodings;

namespace HEAL.HeuristicLib.Operators;

public interface IMutator<TGenotype, in TEncoding> : IOperator
  where TEncoding : IEncoding<TGenotype, TEncoding>
{
  TGenotype Mutate<TContext>(TGenotype parent, TContext context) 
    where TContext : IEncodingContext<TEncoding>, IRandomContext;
}

// public static class Mutator {
//   public static IMutator<TGenotype> Create<TGenotype>(Func<TGenotype, TGenotype> mutator) => new Mutator<TGenotype>(mutator);
// }

// public sealed class Mutator<TGenotype> : IMutator<TGenotype> {
//   private readonly Func<TGenotype, TGenotype> mutator;
//   internal Mutator(Func<TGenotype, TGenotype> mutator) {
//     this.mutator = mutator;
//   }
//   public TGenotype Mutate(TGenotype parent) => mutator(parent);
// }

public abstract class MutatorBase<TGenotype, TEncoding> : IMutator<TGenotype, TEncoding> where TEncoding : IEncoding<TGenotype, TEncoding> {
  public abstract TGenotype Mutate<TContext>(TGenotype parent, TContext context) where TContext : IEncodingContext<TEncoding>, IRandomContext;
}

// public interface IAdaptableMutator<TSolution> : IMutator<TSolution> {
//   TSolution Mutate(TSolution parent, double mutationStrength);
// }
