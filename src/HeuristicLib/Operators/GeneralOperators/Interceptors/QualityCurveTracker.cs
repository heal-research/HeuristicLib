using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public class QualityCurveTracker<TGenotype> {
  public readonly List<(Solution<TGenotype> best, int evalCount)> CurrentState = [];

  private readonly Lock locker = new();
  private Solution<TGenotype>? best;
  private int evalCount;

  public void TrackEvaluation(TGenotype genotype, ObjectiveVector q, IComparer<ObjectiveVector> comparer) {
    lock (locker) {
      evalCount++;
      if (best is not null && comparer.Compare(q, best.ObjectiveVector) >= 0)
        return;
      best = new Solution<TGenotype>(genotype, q);
      CurrentState.Add((best, evalCount));
    }
  }

  public QualityCurveEvaluationWrapper<TGenotype, TEncoding, TProblem> WrapEvaluator<TEncoding, TProblem>(IEvaluator<TGenotype, TEncoding, TProblem> problem)
    where TEncoding : class, IEncoding<TGenotype> where TProblem : IProblem<TGenotype, TEncoding> => new(problem, this);
}
