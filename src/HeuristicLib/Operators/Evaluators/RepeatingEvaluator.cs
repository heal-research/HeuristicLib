using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public static class RepeatingEvaluator
{
    extension<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator) where TSearchSpace : class, ISearchSpace<TCandidate> where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public RepeatingEvaluator<TCandidate, TSearchSpace, TProblem> AsRepeatingAggregating(int repeats, Func<ObjectiveVector, ObjectiveVector, ObjectiveVector> aggregator) =>
            new(evaluator, repeats, aggregator);

        public RepeatedEvaluator<TCandidate, TSearchSpace, TProblem> AsRepeated(int repeats, Func<ObjectiveVector[], ObjectiveVector> aggregator) =>
            new(evaluator, repeats, aggregator);

        public RepeatedEvaluator<TCandidate, TSearchSpace, TProblem> AsRepeated(int repeats, Func<ObjectiveVector[], ObjectiveVector> aggregator, ExecutionConcurrency concurrency) =>
            new(evaluator, repeats, aggregator, concurrency);
    }
}

public record RepeatingEvaluator<TCandidate, TSearchSpace, TProblem>
    : WrappingEvaluator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    private readonly int repeats;
    private readonly Func<ObjectiveVector, ObjectiveVector, ObjectiveVector> aggregator;

    public RepeatingEvaluator(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator, int repeats, Func<ObjectiveVector, ObjectiveVector, ObjectiveVector> aggregator)
        : base(evaluator)
    {
        this.repeats = repeats;
        this.aggregator = aggregator;
    }

    protected override WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem> CreateEvaluatorInstance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> innerEvaluator) =>
        new Instance(innerEvaluator, repeats, aggregator);

    private sealed class Instance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> innerEvaluator, int repeats, Func<ObjectiveVector, ObjectiveVector, ObjectiveVector> aggregator)
        : WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem>(innerEvaluator)
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var results = InnerEvaluator.Evaluate(candidates, random, searchSpace, problem).ToArray();

            for (int i = 0; i < repeats; i++)
            {
                var reevaluationResult = InnerEvaluator.Evaluate(candidates, random, searchSpace, problem);
                for (var j = 0; j < results.Length; j++)
                {
                    results[j] = aggregator(results[j], reevaluationResult[j]);
                }
            }

            return results;
        }
    }
}

public record RepeatedEvaluator<TCandidate, TSearchSpace, TProblem>
    : WrappingEvaluator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    private readonly int repeats;
    private readonly Func<ObjectiveVector[], ObjectiveVector> aggregator;
    private readonly ExecutionConcurrency concurrency;

    public RepeatedEvaluator(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator, int repeats, Func<ObjectiveVector[], ObjectiveVector> aggregator)
        : this(evaluator, repeats, aggregator, ExecutionConcurrency.Sequential()) { }

    public RepeatedEvaluator(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator, int repeats, Func<ObjectiveVector[], ObjectiveVector> aggregator, ExecutionConcurrency concurrency)
      : base(evaluator)
    {
        this.repeats = repeats;
        this.aggregator = aggregator;
        this.concurrency = concurrency;
    }

    protected override WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem> CreateEvaluatorInstance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> innerEvaluator) =>
        new Instance(innerEvaluator, repeats, aggregator, concurrency);

    private sealed class Instance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> innerEvaluator, int repeats, Func<ObjectiveVector[], ObjectiveVector> aggregator, ExecutionConcurrency concurrency)
        : WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem>(innerEvaluator)
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var res = BatchExecution.Execute(repeats, r => InnerEvaluator.Evaluate(candidates, r, searchSpace, problem), random, concurrency);
            return Enumerable.Range(0, candidates.Count)
                .Select(i => Enumerable.Range(0, repeats).Select(j => res[j][i]).ToArray())
                .Select(aggregator)
                .ToArray();
        }
    }
}
