using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

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
            var repeatedObjectives = BatchExecution.Execute(
                repetitions,
                (instance: this, candidates, searchSpace, problem),
                static (itemRandom, state) => state.instance.ChildEvaluator.Evaluate(state.candidates, itemRandom, state.searchSpace, state.problem),
                random,
                concurrency);

            return Enumerable.Range(0, candidates.Count)
                .Select(candidateIndex => aggregator.Aggregate(
                    repeatedObjectives.Select(objectives => objectives[candidateIndex]).ToArray(),
                    problem.Objective))
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
