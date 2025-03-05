namespace HEAL.HeuristicLib.Operators;

public interface ICreator<out TGenotype> : IOperator {
  TGenotype Create();
}

public abstract class CreatorBase<TGenotype> : ICreator<TGenotype> {
  public abstract TGenotype Create();
}

