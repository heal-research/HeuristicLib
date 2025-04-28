using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public abstract record class Mutator<TGenotype, TEncoding> : Operator<IMutatorInstance<TGenotype, TEncoding>>
  where TEncoding : IEncoding<TGenotype>
{
}

public interface IMutatorInstance<TGenotype, in TEncoding> 
  where TEncoding : IEncoding<TGenotype> 
{
  TGenotype Mutate(TGenotype parent, TEncoding encoding, IRandomNumberGenerator random);
}

public abstract class MutatorInstance<TGenotype, TEncoding, TMutator> : OperatorInstance<TMutator>, IMutatorInstance<TGenotype, TEncoding>
  where TEncoding : IEncoding<TGenotype>
{
  protected MutatorInstance(TMutator parameters) : base(parameters) { }
  public abstract TGenotype Mutate(TGenotype parent, TEncoding encoding, IRandomNumberGenerator random);
}
//
// public static class Mutator {
//   public static CustomMutator<TGenotype, TEncoding> Create<TGenotype, TEncoding>(Func<TGenotype, TEncoding, IRandomNumberGenerator, TGenotype> mutator) 
//     where TEncoding : IEncoding<TGenotype>
//   {
//     return new CustomMutator<TGenotype, TEncoding>(mutator);
//   }
// }
//
// public sealed class CustomMutator<TGenotype, TEncoding> 
//   : IMutator<TGenotype, TEncoding>
//   where TEncoding : IEncoding<TGenotype>
// {
//   private readonly Func<TGenotype, TEncoding, IRandomNumberGenerator, TGenotype> mutator;
//   internal CustomMutator(Func<TGenotype, TEncoding, IRandomNumberGenerator, TGenotype> mutator) {
//     this.mutator = mutator;
//   }
//   public TGenotype Mutate(TGenotype parent, TEncoding encoding, IRandomNumberGenerator random) => mutator(parent, encoding, random);
// }
//
// public abstract class MutatorBase<TGenotype, TEncoding>
//   : IMutator<TGenotype, TEncoding>
//   where TEncoding : IEncoding<TGenotype>
// {
//   public abstract TGenotype Mutate(TGenotype parent, TEncoding encoding, IRandomNumberGenerator random);
// }
