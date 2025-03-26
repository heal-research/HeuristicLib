namespace HEAL.HeuristicLib.Operators;

public interface ICreatorOperator<out TGenotype> : IExecutableOperator {
  TGenotype Create();
}

public static class CreatorOperator {
  public static CreatorOperator<TGenotype> Create<TGenotype>(Func<TGenotype> creator) => new CreatorOperator<TGenotype>(creator);
}

public sealed class CreatorOperator<TGenotype> : ICreatorOperator<TGenotype> {
  private readonly Func<TGenotype> creator;
  internal CreatorOperator(Func<TGenotype> creator) {
    this.creator = creator;
  }
  public TGenotype Create() => creator();
}

public abstract class CreatorOperatorBase<TGenotype> : ICreatorOperator<TGenotype> {
  public abstract TGenotype Create();
}
