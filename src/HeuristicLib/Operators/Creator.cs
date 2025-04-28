using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public abstract record class Creator<TGenotype, TEncoding> : Operator<ICreatorInstance<TGenotype, TEncoding>>
  where TEncoding : IEncoding<TGenotype>
{
}


public interface ICreatorInstance<TGenotype, in TEncoding>
  where TEncoding : IEncoding<TGenotype>
{
  TGenotype Create(TEncoding encoding, IRandomNumberGenerator random);
}


public abstract class CreatorInstance<TGenotype, TEncoding, TCreator> : OperatorInstance<TCreator>, ICreatorInstance<TGenotype, TEncoding> 
  where TEncoding : IEncoding<TGenotype>
{
  protected CreatorInstance(TCreator parameters) : base(parameters) { }
  public abstract TGenotype Create(TEncoding encoding, IRandomNumberGenerator random);
}

// public static class Creator {
//   public static CustomCreator<TGenotype, TEncoding> Create<TGenotype, TEncoding>(Func<TEncoding, IRandomNumberGenerator, TGenotype> creator) 
//     where TEncoding : IEncoding<TGenotype> 
//   {
//     return new CustomCreator<TGenotype, TEncoding>(creator);
//   }
// }
//
// public sealed class CustomCreator<TGenotype, TEncoding> 
//   : ICreator<TGenotype, TEncoding> 
//   where TEncoding : IEncoding<TGenotype> {
//   private readonly Func<TEncoding, IRandomNumberGenerator, TGenotype> creator;
//   internal CustomCreator(Func<TEncoding, IRandomNumberGenerator, TGenotype> creator) {
//     this.creator = creator;
//   }
//   public TGenotype Create(TEncoding encoding, IRandomNumberGenerator random) => creator(encoding, random);
// }
//
// public abstract class CreatorBase<TGenotype, TEncoding> : ICreator<TGenotype, TEncoding> where TEncoding : IEncoding<TGenotype> {
//   public abstract TGenotype Create(TEncoding encoding, IRandomNumberGenerator random);
// }
