namespace HEAL.HeuristicLib.Operators;

public interface ICreator<out TGenotype> : IOperator {
  TGenotype Create();
}

public static class Creator {
  public static ICreator<TGenotype> Create<TGenotype>(Func<TGenotype> creator) => new Creator<TGenotype>(creator);
}

public sealed class Creator<TGenotype> : ICreator<TGenotype> {
  private readonly Func<TGenotype> creator;
  internal Creator(Func<TGenotype> creator) {
    this.creator = creator;
  }
  public TGenotype Create() => creator();
}

public abstract class CreatorBase<TGenotype> : ICreator<TGenotype> {
  public abstract TGenotype Create();
}
