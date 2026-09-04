using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// Accumulates the Pareto front over every candidate the observed evaluators produce.
/// </summary>
/// <remarks>
/// This analyzer holds its own data and is used for one run. Its front accumulates across firings rather than being
/// aggregated within one, which is the case a trace-based replacement still has to cover.
/// </remarks>
public sealed class ParetoFrontAnalysis<T, TS, TP> : IAnalyzer
    where TS : class, ISearchSpace<T>
    where TP : class, IProblem<T, TS>
{
    private readonly ImmutableArray<IEvaluator<T, TS, TP>> evaluators;

    public ParetoFrontAnalysis(ObjectiveDirections problemObjective, ObjectiveVector referencePoint, params IReadOnlyList<IEvaluator<T, TS, TP>> evaluators)
    {
        if (evaluators.Count == 0)
            throw new ArgumentException("An analysis needs at least one anchor to observe.", nameof(evaluators));

        this.evaluators = [.. evaluators];
        Front = new ParetoState<T>(referencePoint, problemObjective);
    }

    public ParetoState<T> Front { get; }

    public void Install(ExecutionInstanceResolverBuilder builder)
    {
        foreach (var evaluator in evaluators)
            builder.Observe(evaluator, new ParetoRecorder<T, TS, TP>(Front));
    }
}

/// <summary>
/// Adds every evaluated candidate an evaluator produced to a Pareto front.
/// </summary>
internal sealed class ParetoRecorder<T, TS, TP>(ParetoState<T> front) : IObservationRecorder<EvaluatorObservation<T, TS, TP>>
    where TS : class, ISearchSpace<T>
    where TP : class, IProblem<T, TS>
{
    public void Record(EvaluatorObservation<T, TS, TP> observation) =>
        front.AddPoints(observation.Candidates.ToEvaluated(observation.ObjectiveVectors));
}
