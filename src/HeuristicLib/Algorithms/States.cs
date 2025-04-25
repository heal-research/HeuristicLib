namespace HEAL.HeuristicLib.Algorithms;


public interface IAlgorithmResult {
  [Obsolete("Not necessary on result, since it can be obtained form the algorithm (parameters)")]
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
  TState GetState();
}

public readonly record struct OperatorMetric(int Count, TimeSpan Duration) {
  public static OperatorMetric Aggregate(OperatorMetric left, OperatorMetric right) {
    return new OperatorMetric(left.Count + right.Count, left.Duration + right.Duration);
  }
  public static OperatorMetric operator +(OperatorMetric left, OperatorMetric right) => Aggregate(left, right);
  
  public static OperatorMetric Zero => new(0, TimeSpan.Zero);
}
