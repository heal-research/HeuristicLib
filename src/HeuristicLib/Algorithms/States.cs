namespace HEAL.HeuristicLib.Algorithms;


public interface IAlgorithmResult {
  int UsedRandomSeed { get; }
  TimeSpan TotalDuration { get; }
}

public interface ISingleObjectiveAlgorithmResult<TGenotype> : IAlgorithmResult {
  EvaluatedIndividual<TGenotype>? BestSolution { get; }
}

public interface IMultiObjectiveAlgorithmResult<TGenotype> : IAlgorithmResult {
  IReadOnlyList<EvaluatedIndividual<TGenotype>> ParetoFront { get; }
}

public interface IIterationResult {
  int UsedIterationRandomSeed { get; }
  int Iteration { get; }
  TimeSpan TotalDuration { get; }
}

public interface ISingleObjectiveIterationResult<TGenotype> : IIterationResult {
  EvaluatedIndividual<TGenotype> BestSolution { get; }
}

public interface IMultiObjectiveIterationResult<TGenotype> : IIterationResult {
  IReadOnlyList<EvaluatedIndividual<TGenotype>> ParetoFront { get; }
}

public interface IContinuableIterationResult<out TState> : IIterationResult {
  TState GetNextState(); // ToDo: better name since turning it next sounds like incrementing some count or something
}

public readonly record struct OperatorMetric(int Count, TimeSpan Duration) {
  public static OperatorMetric Aggregate(OperatorMetric left, OperatorMetric right) {
    return new OperatorMetric(left.Count + right.Count, left.Duration + right.Duration);
  }
  public static OperatorMetric operator +(OperatorMetric left, OperatorMetric right) => Aggregate(left, right);
  
  public static OperatorMetric Zero => new(0, TimeSpan.Zero);
}


//
// public interface IStartState {
//   public int Generation { get; }
// }
// public interface IContinuationState : IStartState {}
//
// public interface IResultState {
//   public int Generation { get; }
//   TimeSpan TotalDuration { get; }
// }

// public interface IResultState<TPhenotype> : IResultState {
//   TPhenotype BestResult { get; }
// }


// public interface IContinuableResultState<TPhenotype, out TContinuationState> : IResultState<TPhenotype> where TContinuationState : IContinuationState {
//   TContinuationState GetNextContinuationState();
// }

// public interface IContinuableResultState<out TContinuationState> {
//   TContinuationState GetNextContinuationState();
// }

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
