namespace HEAL.HeuristicLib.Algorithms;

public interface IStartState {
  public int Generation { get; }
}
public interface IContinuationState : IStartState {}

public interface IResultState {
  public int Generation { get; }
  TimeSpan TotalDuration { get; }
}
public interface IContinuableResultState<out TContinuationState> : IResultState where TContinuationState : IContinuationState {
  TContinuationState GetNextContinuationState();
}

//
// public interface IStateTransformer<in TSourceState, TTargetState>
//   where TSourceState : class, IState where TTargetState : class, IState {
//   TTargetState Transform(TSourceState sourceState, TTargetState? previousTargetState = null);
// }
//
// public static class StateTransformer {
//   public static IStateTransformer<TSourceState, TTargetState> Create<TSourceState, TTargetState>(
//     Func<TSourceState, TTargetState?, TTargetState> transform)
//     where TSourceState : class, IState where TTargetState : class, IState 
//   {
//     return new StateTransformer<TSourceState, TTargetState>(transform);
//   }
// }
//
// public sealed class StateTransformer<TSourceState, TTargetState> 
//   : IStateTransformer<TSourceState, TTargetState>
//   where TSourceState : class, IState where TTargetState : class, IState 
// {
//   private readonly Func<TSourceState, TTargetState?, TTargetState> transform;
//   internal StateTransformer(Func<TSourceState, TTargetState?, TTargetState> transform) {
//     this.transform = transform;
//   }
//   public TTargetState Transform(TSourceState sourceState, TTargetState? previousTargetState = null) {
//     return transform(sourceState, previousTargetState);
//   }
// }
