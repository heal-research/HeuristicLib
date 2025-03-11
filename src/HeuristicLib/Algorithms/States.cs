namespace HEAL.HeuristicLib.Algorithms;

public interface IState {}

public interface IPopulationState<out TGenotype, out TObjective> : IState {
  TGenotype[] Population { get; } 
  TObjective[] Objectives { get; }
}

public interface IGenerationalState : IState {
  int Generation { get; }
}

public record PopulationState<TGenotype> : IPopulationState<TGenotype, ObjectiveValue>, IGenerationalState {
  public required int Generation { get; init; }
  public required TGenotype[] Population { get; init; }
  public required ObjectiveValue[] Objectives { get; init; }
}



public interface IStateTransformer<in TSourceState, TTargetState>
  where TSourceState : class, IState where TTargetState : class, IState {
  TTargetState Transform(TSourceState sourceState, TTargetState? previousTargetState = null);
}

public static class StateTransformer {
  public static IStateTransformer<TSourceState, TTargetState> Create<TSourceState, TTargetState>(
    Func<TSourceState, TTargetState?, TTargetState> transform)
    where TSourceState : class, IState where TTargetState : class, IState 
  {
    return new StateTransformer<TSourceState, TTargetState>(transform);
  }
}

public sealed class StateTransformer<TSourceState, TTargetState> 
  : IStateTransformer<TSourceState, TTargetState>
  where TSourceState : class, IState where TTargetState : class, IState 
{
  private readonly Func<TSourceState, TTargetState?, TTargetState> transform;
  internal StateTransformer(Func<TSourceState, TTargetState?, TTargetState> transform) {
    this.transform = transform;
  }
  public TTargetState Transform(TSourceState sourceState, TTargetState? previousTargetState = null) {
    return transform(sourceState, previousTargetState);
  }
}
