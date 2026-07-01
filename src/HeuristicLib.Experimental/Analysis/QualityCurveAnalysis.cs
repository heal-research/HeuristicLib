using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analysis;

public record QualityCurveAnalysis<T, TS, TP, TR> : Analyzer<T, TS, TP, TR, QualityCurve<T>>
  where TS : class, ISearchSpace<T>
  where TP : class, IProblem<T, TS>
  where TR : class, ISearchState

{
    private IEvaluator<T, TS, TP>[] Evaluators { get; }

    public QualityCurveAnalysis(IAlgorithm<T, TS, TP, TR> Algorithm, params IEvaluator<T, TS, TP>[] Evaluators) : base(Algorithm)
    {
        this.Evaluators = Evaluators;
    }

    public void AfterEvaluation(QualityCurve<T> state, IReadOnlyList<Solution<T>> solutions, IProblem<T, TS> problem)
    {
        for (var i = 0; i < solutions.Count; i++)
        {
            var solution = solutions[i];
            var q = solution.ObjectiveVector;
            state.EvalCount++;

            if (state.Best is not null)
            {
                var comp = problem.Objective.TotalOrderComparer;
                if (NoTotalOrderComparer.Instance.Equals(comp))
                {
                    comp = new LexicographicComparer(problem.Objective.Directions);
                }

                if (comp.Compare(q, state.Best.ObjectiveVector) >= 0)
                {
                    continue;
                }
            }

            state.Add(solution);
        }
    }

    public override QualityCurve<T> CreateInitialResult() => new();

    public override void RegisterObservations(ObservationPlan observations, QualityCurve<T> curve)
    {
        foreach (var evaluator in Evaluators)
        {
            observations.Observe(evaluator, (_, solutions, _, problem) => AfterEvaluation(curve, solutions, problem));
        }
    }
}

public sealed class QualityCurve<TGenotype>
{
    private readonly List<(Solution<TGenotype> best, int evalCount)> currentState = [];
    public IReadOnlyList<(Solution<TGenotype> best, int evalCount)> CurrentState => currentState;

    public void Add(Solution<TGenotype> solution)
    {
        Best = solution;
        currentState.Add((solution, EvalCount));
    }

    public int EvalCount { get; set; }
    public Solution<TGenotype>? Best { get; private set; }
}
