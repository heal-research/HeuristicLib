using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analysis;

public record QualityCurveAnalysis<TCandidate, TSearchSpace, TProblem> : Analyzer<QualityCurve<TCandidate>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>

{
    private IEvaluator<TCandidate, TSearchSpace, TProblem>[] Evaluators { get; }

    public QualityCurveAnalysis(params IEvaluator<TCandidate, TSearchSpace, TProblem>[] Evaluators)
    {
        this.Evaluators = Evaluators;
    }

    public void AfterEvaluation(QualityCurve<TCandidate> state, IReadOnlyList<TCandidate> candidates,
                                IReadOnlyList<ObjectiveVector> objectiveVectors,
                                IProblem<TCandidate, ISearchSpace<TCandidate>> problem)
    {
        for (var i = 0; i < candidates.Count; i++)
        {
            var candidate = candidates[i];
            var objectiveVector = objectiveVectors[i];
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

            state.Add(new EvaluatedCandidate<TCandidate>(candidate, objectiveVector));
        }
    }

    public override QualityCurve<TCandidate> CreateInitialResult() => new();

    public override void RegisterObservations(ObservationPlan observations, QualityCurve<TCandidate> curve)
    {
        foreach (var evaluator in Evaluators)
        {
            observations.Observe(evaluator,
                (candidates, objectiveVectors, _, problem) =>
                    AfterEvaluation(curve, candidates, objectiveVectors, problem));
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
