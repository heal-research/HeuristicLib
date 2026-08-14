using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public record BestPerEvaluationAnalysis<TCandidate, TSearchSpace, TProblem> : Analyzer<QualityCurve<TCandidate>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>

{
    private ImmutableArray<IEvaluator<TCandidate, TSearchSpace, TProblem>> Evaluators { get; }

    public BestPerEvaluationAnalysis(params IReadOnlyList<IEvaluator<TCandidate, TSearchSpace, TProblem>> evaluators)
    {
        Evaluators = evaluators.ToImmutableArray();
    }

    public void AfterEvaluation(QualityCurve<TCandidate> state,
                                IReadOnlyList<EvaluatedCandidate<TCandidate>> evaluatedCandidates,
                                IProblem<TCandidate, ISearchSpace<TCandidate>> problem)
    {
        foreach (var evaluatedCandidate in evaluatedCandidates)
        {
            var objectiveVector = evaluatedCandidate.ObjectiveVector;
            state.EvalCount++;

            if (state.Best is not null)
            {
                var comp = problem.Objective.TotalOrderComparer;
                if (NoTotalOrderComparer.Instance.Equals(comp))
                {
                    comp = new LexicographicComparer(problem.Objective.Directions);
                }

                if (comp.Compare(objectiveVector, state.Best.ObjectiveVector) >= 0)
                {
                    continue;
                }
            }

            state.Add(evaluatedCandidate);
        }
    }

    public override QualityCurve<TCandidate> CreateInitialResult() => new();

    public override void RegisterObservations(ObservationPlan observations, QualityCurve<TCandidate> curve)
    {
        foreach (var evaluator in Evaluators)
        {
            observations.Observe(evaluator,
                (_, evaluatedCandidates, _, problem) =>
                    AfterEvaluation(curve, evaluatedCandidates, problem));
        }
    }
}

public sealed class QualityCurve<TCandidate>
{
    private readonly List<(EvaluatedCandidate<TCandidate> best, int evalCount)> currentState = [];
    public IReadOnlyList<(EvaluatedCandidate<TCandidate> best, int evalCount)> CurrentState => currentState;

    public void Add(EvaluatedCandidate<TCandidate> solution)
    {
        Best = solution;
        currentState.Add((solution, EvalCount));
    }

    public int EvalCount { get; set; }
    public EvaluatedCandidate<TCandidate>? Best { get; private set; }
}
