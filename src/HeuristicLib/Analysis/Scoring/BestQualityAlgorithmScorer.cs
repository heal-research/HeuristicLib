using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analysis.Scoring;

public record BestQualityAlgorithmScorer<TCandidate, TSearchSpace, TProblem, TSearchState> : AlgorithmPerformanceEvaluator<TCandidate, TSearchSpace, TProblem, TSearchState, QualityScorerState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : class, ISearchState
{
    public BestQualityAlgorithmScorer(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> Algorithm, ObjectiveDirections Objective, params IEvaluator<TCandidate, TSearchSpace, TProblem>[] Evaluator) : base(Algorithm)
    {
        this.Objective = Objective;
        this.Evaluator = Evaluator;
    }

    public override QualityScorerState CreateInitialResult() => new() { CurrentScore = Objective.Worst };

    public override void RegisterObservations(ObservationPlan observations, QualityScorerState result)
    {
        foreach (var evaluator in Evaluator)
        {
            observations.Observe(evaluator,
              (_, objectives, _, _) => result.CurrentScore = objectives.OrderBy(x => x, Objective.TotalOrderComparer).First());
        }
    }

    public override ObjectiveDirections Objective { get; }
    private IEvaluator<TCandidate, TSearchSpace, TProblem>[] Evaluator { get; }
}

public record HyperVolumeAlgorithmScorer<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> Algorithm, ObjectiveDirections ProblemObjective, ObjectiveVector ReferencePoint, params IEvaluator<TCandidate, TSearchSpace, TProblem>[] Evaluator)
  : AlgorithmPerformanceEvaluator<TCandidate, TSearchSpace, TProblem, TSearchState, HyperVolumeAlgorithmScorer<TCandidate, TSearchSpace, TProblem, TSearchState>.State>(Algorithm)
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : class, ISearchState
{
    public override State CreateInitialResult() => new();

    public override void RegisterObservations(ObservationPlan observations, State result)
    {
        foreach (var evaluator in Evaluator)
        {
            observations.Observe(evaluator,
              (candidates, objectives, _, _) =>
              {
                  result.AddPoints(candidates.Zip(objectives).Select(x => new EvaluatedCandidate<TCandidate>(x.First, x.Second)), ProblemObjective, ReferencePoint);
              });
        }
    }

    public override ObjectiveDirections Objective { get; } = SingleObjective.Maximize;

    public class State : ParetoState<TCandidate>, IAlgorithmPerformanceState
    {
        public ObjectiveVector CurrentScore => HyperVolume?.Value ?? 0;
    }
}

public class ParetoState<TCandidate>
{
    protected Lazy<ObjectiveVector>? HyperVolume;
    private List<EvaluatedCandidate<TCandidate>> Front { get; } = [];

    public void AddPoints(IEnumerable<EvaluatedCandidate<TCandidate>> solutions, ObjectiveDirections objective, ObjectiveVector referencePoint)
    {
        var TCandidate = false;
        foreach (var solution in solutions)
        {
            if (DominationCalculator.TryAddToParetoFrontInPlace(Front, solution, objective))
                TCandidate = true;
        }

        if (!TCandidate)
            return;
        HyperVolume = new Lazy<ObjectiveVector>(() => HyperVolumeCalculator.Calculate(Front.Select(x => x.ObjectiveVector), referencePoint, objective));
    }

    public void Clear()
    {
        Front.Clear();
        HyperVolume = null;
    }
}
