using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public static class RepeatingEvaluator
{
    public static RepeatingEvaluator<TCandidate, TSearchSpace, TProblem> AsRepeatingAggregating<TCandidate, TSearchSpace, TProblem>(
      this IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator,
      int repeats,
      Func<ObjectiveVector, ObjectiveVector, ObjectiveVector> aggregator)
      where TSearchSpace : class, ISearchSpace<TCandidate> where TProblem : class, IProblem<TCandidate, TSearchSpace> => new(evaluator, repeats, aggregator);

    public static RepeatedEvaluator<TCandidate, TSearchSpace, TProblem> AsRepeated<TCandidate, TSearchSpace, TProblem>(
        this IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator,
        int repeats,
        Func<ReadOnlySpan<ObjectiveVector>, ObjectiveVector>? aggregator = null,
        IEqualityComparer<TCandidate>? comparer = null,
        int maxDegreeOfParallelism = -1)
      where TSearchSpace : class, ISearchSpace<TCandidate> where TProblem : class, IProblem<TCandidate, TSearchSpace>
      => new(evaluator, repeats, aggregator, comparer, maxDegreeOfParallelism);
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
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var results = InnerEvaluator.Evaluate(candidates, random, searchSpace, problem).ToArray();

            for (var i = 0; i < repeats; i++)
            {
                var reevaluationResult = InnerEvaluator.Evaluate(candidates, random, searchSpace, problem);
                for (var j = 0; j < results.Length; j++)
                {
                    results[j] = results[j] with
                    {
                        ObjectiveVector = aggregator(results[j].ObjectiveVector, reevaluationResult[j].ObjectiveVector)
                    };
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
    private readonly Func<ReadOnlySpan<ObjectiveVector>, ObjectiveVector> aggregator;
    private readonly IEqualityComparer<TCandidate> comparer;
    private readonly int maxDegreeOfParallelism;

    public RepeatedEvaluator(
        IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator,
        int repeats,
        Func<ReadOnlySpan<ObjectiveVector>, ObjectiveVector>? aggregator = null,
        IEqualityComparer<TCandidate>? comparer = null,
        int maxDegreeOfParallelism = -1)
      : base(evaluator)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(repeats);
        this.repeats = repeats;
        this.aggregator = aggregator ?? Mean;
        this.comparer = comparer ?? EqualityComparer<TCandidate>.Default;
        this.maxDegreeOfParallelism = maxDegreeOfParallelism;
    }

    protected override WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem> CreateEvaluatorInstance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> innerEvaluator) =>
        new Instance(innerEvaluator, repeats, aggregator, comparer, maxDegreeOfParallelism);

    private sealed class Instance(
        IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> innerEvaluator,
        int repeats,
        Func<ReadOnlySpan<ObjectiveVector>, ObjectiveVector> aggregator,
        IEqualityComparer<TCandidate> comparer,
        int maxDegreeOfParallelism)
        : WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem>(innerEvaluator)
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var evaluations = BatchExecution.Parallel(
                repeats,
                r => InnerEvaluator.Evaluate(candidates, r, searchSpace, problem),
                random,
                maxDegreeOfParallelism: maxDegreeOfParallelism);

            var objectiveVectors = new ObjectiveVector[repeats];
            return Enumerable.Range(0, candidates.Count)
                .Select(candidateIndex =>
                {
                    var evaluatedCandidate = evaluations[0][candidateIndex];
                    for (var repetition = 1; repetition < repeats; repetition++)
                    {
                        EnsureSameCandidate(
                            evaluatedCandidate.Candidate,
                            evaluations[repetition][candidateIndex].Candidate,
                            comparer);
                    }

                    for (var repetition = 0; repetition < repeats; repetition++)
                    {
                        objectiveVectors[repetition] = evaluations[repetition][candidateIndex].ObjectiveVector;
                    }

                    return evaluatedCandidate with { ObjectiveVector = aggregator(objectiveVectors) };
                })
                .ToArray();
        }
    }

    private static ObjectiveVector Mean(ReadOnlySpan<ObjectiveVector> objectiveVectors)
    {
        var dimension = objectiveVectors[0].Count;
        var values = new double[dimension];
        foreach (var objectiveVector in objectiveVectors)
        {
            if (objectiveVector.Count != dimension)
            {
                throw new InvalidOperationException("Repeated evaluator aggregation requires all objective vectors to have the same dimension.");
            }

            for (var i = 0; i < dimension; i++)
            {
                values[i] += objectiveVector[i];
            }
        }

        for (var i = 0; i < values.Length; i++)
        {
            values[i] /= objectiveVectors.Length;
        }

        return new ObjectiveVector(values);
    }

    private static void EnsureSameCandidate(
        TCandidate first,
        TCandidate second,
        IEqualityComparer<TCandidate> candidateComparer)
    {
        if (!candidateComparer.Equals(first, second))
        {
            throw new InvalidOperationException(
                "Repeated evaluator aggregation requires all repeated evaluations of one input slot to return the same candidate.");
        }
    }
}
