using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analysis;

public record BestQualityAlgorithmScorer<T, TS, TP, TSearchState> : AlgorithmPerformanceEvaluator<T, TS, TP, TSearchState, QualityScorerState>
  where TS : class, ISearchSpace<T>
  where TP : class, IProblem<T, TS>
  where TSearchState : class, ISearchState
{
  public BestQualityAlgorithmScorer(IAlgorithm<T, TS, TP, TSearchState> Algorithm, Objective Objective, params IEvaluator<T, TS, TP>[] Evaluator) : base(Algorithm)
  {
    this.Objective = Objective;
    this.Evaluator = Evaluator;
  }

  public override QualityScorerState CreateInitialState() => new() { CurrentScore = Objective.Worst };

  public override void RegisterObservations(IObservationRegistry observationRegistry, QualityScorerState state)
  {
    foreach (var evaluator in Evaluator) {
      observationRegistry.Add(evaluator,
        (_, objectives, _, _) => state.CurrentScore = objectives.OrderBy(x => x, Objective.TotalOrderComparer).First());
    }
  }

  public override Objective Objective { get; }
  private IEvaluator<T, TS, TP>[] Evaluator { get; }
}

public record HyperVolumeAlgorithmScorer<T, TS, TP, TSearchState>(IAlgorithm<T, TS, TP, TSearchState> Algorithm, Objective ProblemObjective, ObjectiveVector ReferencePoint, params IEvaluator<T, TS, TP>[] Evaluator)
  : AlgorithmPerformanceEvaluator<T, TS, TP, TSearchState, HyperVolumeAlgorithmScorer<T, TS, TP, TSearchState>.State>(Algorithm)
  where TS : class, ISearchSpace<T>
  where TP : class, IProblem<T, TS>
  where TSearchState : class, ISearchState
{
  public override State CreateInitialState() => new();

  public override void RegisterObservations(IObservationRegistry observationRegistry, State state)
  {
    foreach (var evaluator in Evaluator) {
      observationRegistry.Add(evaluator,
        (genotypes, objectives, _, _) => {
          state.AddPoints(genotypes.Zip(objectives).Select(x => new Solution<T>(x.First, x.Second)), ProblemObjective, ReferencePoint);
        });
    }
  }

  public override Objective Objective { get; } = SingleObjective.Maximize;

  public class State : ParetoState<T>, IAlgorithmPerformanceState
  {
    public ObjectiveVector CurrentScore => HyperVolume?.Value ?? 0;
  }
}

public class ParetoState<T>
{
  protected Lazy<ObjectiveVector>? HyperVolume;
  private List<ISolution<T>> Front { get; } = [];

  public void AddPoints(IEnumerable<ISolution<T>> solutions, Objective objective, ObjectiveVector referencePoint)
  {
    var t = false;
    foreach (var solution in solutions) {
      if (DominationCalculator.TryAddToParetoFrontInPlace(Front, solution, objective))
        t = true;
    }

    if (!t) return;
    HyperVolume = new Lazy<ObjectiveVector>(() => HyperVolumeCalculator.Calculate(Front.Select(x => x.ObjectiveVector), referencePoint, objective));
  }

  public void Clear()
  {
    Front.Clear();
    HyperVolume = null;
  }
}
