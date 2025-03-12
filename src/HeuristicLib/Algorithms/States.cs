namespace HEAL.HeuristicLib.Algorithms;

public interface IState {}

public interface IGenerationalState : IState {
  int Generation { get; }
  
  IGenerationalState Next();
  IGenerationalState Reset();
}

public static class StateExtensions {
  public static TState Next<TState>(this TState state) where TState : IGenerationalState {
    return (TState)state.Next();
  }
  public static TState Reset<TState>(this TState state) where TState : IGenerationalState {
    return (TState)state.Reset();
  }
}

public record PopulationState<TGenotype, TFitness, TGoal> : IGenerationalState {
  public int Generation { get; init; } = 0;
  //public int TotalGeneration { get; init; }
  
  public required TGoal Goal { get; init; }
  public required Phenotype<TGenotype, TFitness>[] Population { get; init; }
  
  //public TimeSpan TotalDuration { get; init; }
  //public TimeSpan EvaluationDuration { get; init; }
  // others
  
  //public int NumberOfEvaluations { get; init; }
  // public int NumberOfSelectedParents { get; init; }
  // public int NumberOfChildren { get; init; }
  // public int NumberOfCrossovers { get; init; }
  //public int NumberOfMutations { get; init; }

  IGenerationalState IGenerationalState.Next() => this with { Generation = Generation + 1 };
  IGenerationalState IGenerationalState.Reset() => this with { Generation = 0 };

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
