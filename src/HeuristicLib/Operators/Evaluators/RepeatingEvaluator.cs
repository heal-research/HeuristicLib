using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Evaluates every candidate repeatedly and aggregates the resulting objective vectors.
/// </summary>
public sealed record RepeatingEvaluator<TCandidate>
    : WrappingOperator<Evaluation, TCandidate>
{
    /// <summary>
    /// Gets the total number of evaluations performed for each candidate.
    /// </summary>
    public int Repetitions { get; init; }

    public IObjectiveVectorAggregator Aggregator { get; init; } = ObjectiveVectorAggregation.Mean;

    public ExecutionConcurrency Concurrency { get; init; } = ExecutionConcurrency.Sequential();

    public static Parameter<RepeatingEvaluator<TCandidate>, int> RepetitionsParameter { get; } =
        new(nameof(Repetitions), static evaluator => evaluator.Repetitions, static (evaluator, value) => evaluator with { Repetitions = value });

    public static Parameter<RepeatingEvaluator<TCandidate>, IObjectiveVectorAggregator> AggregatorParameter { get; } =
        new(nameof(Aggregator), static evaluator => evaluator.Aggregator, static (evaluator, value) => evaluator with { Aggregator = value });

    public static Parameter<RepeatingEvaluator<TCandidate>, ExecutionConcurrency> ConcurrencyParameter { get; } =
        new(nameof(Concurrency), static evaluator => evaluator.Concurrency, static (evaluator, value) => evaluator with { Concurrency = value });

    public new static IReadOnlyList<IParameter> Parameters { get; } = [RepetitionsParameter, AggregatorParameter, ConcurrencyParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    public RepeatingEvaluator(IOperator<Evaluation, TCandidate> childEvaluator, int repetitions)
        : base(childEvaluator)
    {
        Repetitions = repetitions;
    }

    protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
        ResolutionScope<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
    {
        if (Repetitions <= 0)
            throw new InvalidOperationException("Repetitions must be positive.");
        var childEvaluator = scope.Resolve(ChildOperator);
        return new Executor(childEvaluator, Repetitions, Aggregator, Concurrency, scope.Problem);
    }

    private sealed class Executor(IEvaluationExecutor<TCandidate> childEvaluator, int repetitions, IObjectiveVectorAggregator aggregator, ExecutionConcurrency concurrency, IProblem problem)
        : EvaluationExecutor<TCandidate>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random)
        {
            var repetitionResults = BatchExecution.Execute(
                repetitions,
                (childEvaluator, candidates),
                static (itemRandom, state) => state.childEvaluator.Evaluate(state.candidates, itemRandom),
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
    public static RepeatingEvaluator<TCandidate> Create<TCandidate>(IOperator<Evaluation, TCandidate> childEvaluator, int repetitions) =>
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
    extension<TCandidate>(IOperator<Evaluation, TCandidate> evaluator)
    {
        public RepeatingEvaluator<TCandidate> AsRepeated(int repetitions) =>
            new(evaluator, repetitions);

        public RepeatingEvaluator<TCandidate> AsRepeated(int repetitions, IObjectiveVectorAggregator aggregator) =>
            new(evaluator, repetitions) { Aggregator = aggregator };

        public RepeatingEvaluator<TCandidate> AsRepeated(int repetitions, ExecutionConcurrency concurrency) =>
            new(evaluator, repetitions) { Concurrency = concurrency };

        public RepeatingEvaluator<TCandidate> AsRepeated(int repetitions, IObjectiveVectorAggregator aggregator, ExecutionConcurrency concurrency) =>
            new(evaluator, repetitions) { Aggregator = aggregator, Concurrency = concurrency };
    }
}
