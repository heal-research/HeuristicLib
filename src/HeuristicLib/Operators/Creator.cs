using HEAL.HeuristicLib.Algorithms;

namespace HEAL.HeuristicLib.Operators;

public interface ICreator<out TGenotype> : IOperator {
  TGenotype Create(IRandomNumberGenerator random);
}

public static class Creator {
  public static ICreator<TGenotype> Create<TGenotype>(Func<IRandomNumberGenerator, TGenotype> creator) => new Creator<TGenotype>(creator);
}

public sealed class Creator<TGenotype> : ICreator<TGenotype> {
  private readonly Func<IRandomNumberGenerator, TGenotype> creator;
  internal Creator(Func<IRandomNumberGenerator, TGenotype> creator) {
    this.creator = creator;
  }
  public TGenotype Create(IRandomNumberGenerator random) => creator(random);
}

public abstract class CreatorBase<TGenotype> : ICreator<TGenotype> {
  public abstract TGenotype Create(IRandomNumberGenerator random);
}
