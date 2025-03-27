using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public interface IMutator<TGenotype, in TEncodingParameter>
  : IOperator
  where TEncodingParameter : IEncodingParameter<TGenotype>
{
  TGenotype Mutate(TGenotype parent, TEncodingParameter encoding, IRandomNumberGenerator random);
}

public static class Mutator {
  public static CustomMutator<TGenotype, TEncodingParameter> Create<TGenotype, TEncodingParameter>(Func<TGenotype, TEncodingParameter, IRandomNumberGenerator, TGenotype> mutator) 
    where TEncodingParameter : IEncodingParameter<TGenotype>
  {
    return new CustomMutator<TGenotype, TEncodingParameter>(mutator);
  }
}

public sealed class CustomMutator<TGenotype, TEncodingParameter> 
  : IMutator<TGenotype, TEncodingParameter>
  where TEncodingParameter : IEncodingParameter<TGenotype>
{
  private readonly Func<TGenotype, TEncodingParameter, IRandomNumberGenerator, TGenotype> mutator;
  internal CustomMutator(Func<TGenotype, TEncodingParameter, IRandomNumberGenerator, TGenotype> mutator) {
    this.mutator = mutator;
  }
  public TGenotype Mutate(TGenotype parent, TEncodingParameter encoding, IRandomNumberGenerator random) => mutator(parent, encoding, random);
}

public abstract class MutatorBase<TGenotype, TEncodingParameter>
  : IMutator<TGenotype, TEncodingParameter>
  where TEncodingParameter : IEncodingParameter<TGenotype>
{
  public abstract TGenotype Mutate(TGenotype parent, TEncodingParameter encoding, IRandomNumberGenerator random);
}
