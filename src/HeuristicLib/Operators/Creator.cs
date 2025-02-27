namespace HEAL.HeuristicLib.Operators;

public interface ICreator<out TGenotype> : IOperator {
  TGenotype Create();
}

public interface ICreatorTemplate<out TCreator, TGenotype, in TParams> 
  : IOperatorTemplate<TCreator, TParams>
  where TCreator : ICreator<TGenotype> 
  where TParams : CreatorParameters {
}

public record CreatorParameters();

public abstract class CreatorBase<TGenotype> : ICreator<TGenotype> {
  public abstract TGenotype Create();
}

public abstract class CreatorTemplateBase<TCreator, TGenotype, TParams> 
  : ICreatorTemplate<TCreator, TGenotype, TParams>
  where TCreator : ICreator<TGenotype> 
  where TParams : CreatorParameters {
  public abstract TCreator Parameterize(TParams parameters);
}
