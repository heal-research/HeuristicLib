using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public interface ICreator<out TGenotype, in TEncodingParameter> 
  : IOperator
  where TEncodingParameter : IEncodingParameter<TGenotype>
{
  TGenotype Create(TEncodingParameter encoding, IRandomNumberGenerator random);
}

public static class Creator {
  public static CustomCreator<TGenotype, TEncodingParameter> Create<TGenotype, TEncodingParameter>(Func<TEncodingParameter, IRandomNumberGenerator, TGenotype> creator) 
    where TEncodingParameter : IEncodingParameter<TGenotype> 
  {
    return new CustomCreator<TGenotype, TEncodingParameter>(creator);
  }
}

public sealed class CustomCreator<TGenotype, TEncodingParameter> 
  : ICreator<TGenotype, TEncodingParameter> 
  where TEncodingParameter : IEncodingParameter<TGenotype> {
  private readonly Func<TEncodingParameter, IRandomNumberGenerator, TGenotype> creator;
  internal CustomCreator(Func<TEncodingParameter, IRandomNumberGenerator, TGenotype> creator) {
    this.creator = creator;
  }
  public TGenotype Create(TEncodingParameter encoding, IRandomNumberGenerator random) => creator(encoding, random);
}

public abstract class CreatorBase<TGenotype, TEncodingParameter> : ICreator<TGenotype, TEncodingParameter> where TEncodingParameter : IEncodingParameter<TGenotype> {
  public abstract TGenotype Create(TEncodingParameter encoding, IRandomNumberGenerator random);
}
