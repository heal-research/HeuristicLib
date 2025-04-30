namespace HEAL.HeuristicLib.Algorithms;


public interface IAlgorithmResult {
  int CurrentIteration { get; }
  int TotalIterations { get; }
  
  TimeSpan CurrentDuration { get; }
  TimeSpan TotalDuration { get; }
}

public interface ISingleObjectiveAlgorithmResult<TGenotype> : IAlgorithmResult {
  EvaluatedIndividual<TGenotype>? CurrentBestSolution { get; }
  EvaluatedIndividual<TGenotype>? BestSolution { get; }
}

public interface IMultiObjectiveAlgorithmResult<TGenotype> : IAlgorithmResult {
  IReadOnlyList<EvaluatedIndividual<TGenotype>> CurrentParetoFront { get; }
  IReadOnlyList<EvaluatedIndividual<TGenotype>> ParetoFront { get; }
}

// public interface IIterationResult {
//   int Iteration { get; }
//   TimeSpan TotalDuration { get; }
// }

// public interface ISingleObjectiveIterationResult<TGenotype> : IIterationResult {
//   EvaluatedIndividual<TGenotype> BestSolution { get; }
// }
//
// public interface IMultiObjectiveIterationResult<TGenotype> : IIterationResult {
//   IReadOnlyList<EvaluatedIndividual<TGenotype>> ParetoFront { get; }
// }

public interface IContinuableAlgorithmResult<out TState> : IAlgorithmResult {
  TState GetContinuationState();
  TState GetRestartState();
}

// public interface IContinuableIterationResult<out TState> : IIterationResult {
//   TState GetContinuationState();
//   TState GetRestartState();
// }

public readonly record struct OperatorMetric(int Count, TimeSpan Duration) {
  public static OperatorMetric Aggregate(OperatorMetric left, OperatorMetric right) {
    return new OperatorMetric(left.Count + right.Count, left.Duration + right.Duration);
  }
  public static OperatorMetric operator +(OperatorMetric left, OperatorMetric right) => Aggregate(left, right);
  
  public static OperatorMetric Zero => new(0, TimeSpan.Zero);
}
