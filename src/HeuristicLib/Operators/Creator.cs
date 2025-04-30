using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public abstract record class Creator<TGenotype, TSearchSpace> : Operator<ICreatorInstance<TGenotype, TSearchSpace>>
  where TSearchSpace : ISearchSpace<TGenotype>
{
}


public interface ICreatorInstance<TGenotype, in TSearchSpace>
  where TSearchSpace : ISearchSpace<TGenotype>
{
  TGenotype Create(TSearchSpace searchSpace, IRandomNumberGenerator random);
}


public abstract class CreatorInstance<TGenotype, TSearchSpace, TCreator> : OperatorInstance<TCreator>, ICreatorInstance<TGenotype, TSearchSpace> 
  where TSearchSpace : ISearchSpace<TGenotype>
{
  protected CreatorInstance(TCreator parameters) : base(parameters) { }
  public abstract TGenotype Create(TSearchSpace searchSpace, IRandomNumberGenerator random);
}

// public static class Creator {
//   public static CustomCreator<TGenotype, TSearchSpace> Create<TGenotype, TSearchSpace>(Func<TSearchSpace, IRandomNumberGenerator, TGenotype> creator) 
//     where TSearchSpace : ISearchSpace<TGenotype> 
//   {
//     return new CustomCreator<TGenotype, TSearchSpace>(creator);
//   }
// }
//
// public sealed class CustomCreator<TGenotype, TSearchSpace> 
//   : ICreator<TGenotype, TSearchSpace> 
//   where TSearchSpace : ISearchSpace<TGenotype> {
//   private readonly Func<TSearchSpace, IRandomNumberGenerator, TGenotype> creator;
//   internal CustomCreator(Func<TSearchSpace, IRandomNumberGenerator, TGenotype> creator) {
//     this.creator = creator;
//   }
//   public TGenotype Create(TSearchSpace searchSpace, IRandomNumberGenerator random) => creator(searchSpace, random);
// }
//
// public abstract class CreatorBase<TGenotype, TSearchSpace> : ICreator<TGenotype, TSearchSpace> where TSearchSpace : ISearchSpace<TGenotype> {
//   public abstract TGenotype Create(TSearchSpace searchSpace, IRandomNumberGenerator random);
// }
