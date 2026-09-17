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
/// This analyzer holds its own data and is used for one run. It merges observations from several evaluators
/// into one front, rather than retaining a front snapshot after every observation.
/// </remarks>
public sealed class ParetoFrontAnalyzer<T, TS, TP> : IAnalyzer
    where TS : class, ISearchSpace<T>
    where TP : class, IProblem<T, TS>
{
    private readonly ImmutableArray<IEvaluator<T, TS, TP>> evaluators;

    public ParetoFrontAnalyzer(ObjectiveDirections problemObjective, ObjectiveVector referencePoint, params IReadOnlyList<IEvaluator<T, TS, TP>> evaluators)
    {
        if (evaluators.Count == 0)
            throw new ArgumentException("An analyzer needs at least one observation source.", nameof(evaluators));

        this.evaluators = [.. evaluators];
        Front = new ParetoState<T>(referencePoint, problemObjective);
    }

    public ParetoState<T> Front { get; }

    public void Install(ExecutionInstanceResolverBuilder builder)
    {
        foreach (var evaluator in evaluators)
            builder.Observe(evaluator, Record);
    }

    private void Record(EvaluatorObservation<T, TS, TP> observation) =>
        Front.AddPoints(observation.Candidates.ToEvaluated(observation.ObjectiveVectors));
}
