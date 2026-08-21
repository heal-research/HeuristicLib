using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Evaluates every candidate repeatedly and aggregates the resulting objective vectors.
/// </summary>
public sealed record RepeatingEvaluator<TCandidate, TSearchSpace, TProblem>
    : WrappingEvaluator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    /// <summary>
    /// Gets the total number of evaluations performed for each candidate.
    /// </summary>
    public int Repetitions { get; init; }

    /// <summary>
    /// Gets the strategy used to aggregate the repeated objective vectors.
    /// </summary>
    public IObjectiveVectorAggregator Aggregator { get; init; } = ObjectiveVectorAggregation.Mean;

    /// <summary>
    /// Gets the concurrency used to execute the repeated evaluation batches.
    /// </summary>
    public ExecutionConcurrency Concurrency { get; init; } = ExecutionConcurrency.Sequential();

    public RepeatingEvaluator(IEvaluator<TCandidate, TSearchSpace, TProblem> childEvaluator, int repetitions)
        : base(childEvaluator)
    {
        Repetitions = repetitions;
    }

    protected override WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> childEvaluator)
    {
        if (Repetitions <= 0)
            throw new InvalidOperationException("Repetitions must be positive.");

        return new Instance(childEvaluator, Repetitions, Aggregator, Concurrency);
    }

    private sealed class Instance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> childEvaluator, int repetitions, IObjectiveVectorAggregator aggregator, ExecutionConcurrency concurrency)
        : WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem>(childEvaluator)
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var repetitionResults = BatchExecution.Execute(
                repetitions,
                (instance: this, candidates, searchSpace, problem),
                static (itemRandom, state) => state.instance.ChildEvaluator.Evaluate(state.candidates, itemRandom, state.searchSpace, state.problem),
                random,
                concurrency);

            return Enumerable.Range(0, candidates.Count)
                .Select(candidateIndex =>
                {
                    var objectiveVectors = new ObjectiveVector[repetitions];
                    for (var repetition = 0; repetition < repetitions; repetition++)
                        objectiveVectors[repetition] = repetitionResults[repetition][candidateIndex];

                    return aggregator.Aggregate(objectiveVectors, problem.Objective);
                })
                .ToArray();
        }
    }
}

public static class RepeatingEvaluator
{
    public static RepeatingEvaluator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate, TSearchSpace, TProblem> childEvaluator, int repetitions)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childEvaluator, repetitions);
}

/// <summary>
/// Aggregates the objective vectors produced by repeated evaluations of one candidate.
/// </summary>
public interface IObjectiveVectorAggregator
{
    /// <summary>
    /// Aggregates repeated objective vectors into one objective vector.
    /// </summary>
    /// <param name="objectiveVectors">The objective vectors produced for one candidate. The collection is never empty.</param>
    /// <param name="objectiveDirections">The problem's objective directions and total objective order.</param>
    ObjectiveVector Aggregate(IReadOnlyList<ObjectiveVector> objectiveVectors, ObjectiveDirections objectiveDirections);
}

public static class ObjectiveVectorAggregation
{
    public static MeanObjectiveVectorAggregator Mean { get; } = new();
    public static MedianObjectiveVectorAggregator Median { get; } = new();
    public static BestObjectiveVectorAggregator Best { get; } = new();
    public static WorstObjectiveVectorAggregator Worst { get; } = new();
}

public sealed record MeanObjectiveVectorAggregator : IObjectiveVectorAggregator
{
    public ObjectiveVector Aggregate(IReadOnlyList<ObjectiveVector> objectiveVectors, ObjectiveDirections objectiveDirections) =>
        objectiveVectors.Mean();
}

public sealed record MedianObjectiveVectorAggregator : IObjectiveVectorAggregator
{
    public ObjectiveVector Aggregate(IReadOnlyList<ObjectiveVector> objectiveVectors, ObjectiveDirections objectiveDirections) =>
        objectiveVectors.Median(objectiveDirections);
}

public sealed record BestObjectiveVectorAggregator : IObjectiveVectorAggregator
{
    public ObjectiveVector Aggregate(IReadOnlyList<ObjectiveVector> objectiveVectors, ObjectiveDirections objectiveDirections) =>
        objectiveVectors.Best(objectiveDirections);
}

public sealed record WorstObjectiveVectorAggregator : IObjectiveVectorAggregator
{
    public ObjectiveVector Aggregate(IReadOnlyList<ObjectiveVector> objectiveVectors, ObjectiveDirections objectiveDirections) =>
        objectiveVectors.Worst(objectiveDirections);
}

public static class RepeatingEvaluatorExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public RepeatingEvaluator<TCandidate, TSearchSpace, TProblem> AsRepeated(int repetitions) =>
            new(evaluator, repetitions);

        public RepeatingEvaluator<TCandidate, TSearchSpace, TProblem> AsRepeated(int repetitions, IObjectiveVectorAggregator aggregator) =>
            new(evaluator, repetitions) { Aggregator = aggregator };

        public RepeatingEvaluator<TCandidate, TSearchSpace, TProblem> AsRepeated(int repetitions, ExecutionConcurrency concurrency) =>
            new(evaluator, repetitions) { Concurrency = concurrency };

        public RepeatingEvaluator<TCandidate, TSearchSpace, TProblem> AsRepeated(int repetitions, IObjectiveVectorAggregator aggregator, ExecutionConcurrency concurrency) =>
            new(evaluator, repetitions) { Aggregator = aggregator, Concurrency = concurrency };
    }
}
