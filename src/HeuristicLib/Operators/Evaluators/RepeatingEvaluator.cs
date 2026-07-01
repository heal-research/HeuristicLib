using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public record RepeatedEvaluator<TGenotype, TSearchSpace, TProblem>
    : WrappingEvaluator<TGenotype, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
    private readonly int repeats;
    private readonly Func<ReadOnlySpan<ObjectiveVector>, ObjectiveVector> aggregator;
    private readonly IEqualityComparer<TGenotype> comparer;
    private readonly int maxDegreeOfParallelism;

    public RepeatedEvaluator(
        IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator,
        int repeats,
        Func<ReadOnlySpan<ObjectiveVector>, ObjectiveVector>? aggregator = null,
        IEqualityComparer<TGenotype>? comparer = null,
        int maxDegreeOfParallelism = -1)
        : base(evaluator)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(repeats);
        this.repeats = repeats;
        this.aggregator = aggregator ?? Mean;
        this.comparer = comparer ?? EqualityComparer<TGenotype>.Default;
        this.maxDegreeOfParallelism = maxDegreeOfParallelism;
    }

    protected override IReadOnlyList<Solution<TGenotype>> Evaluate(
        IReadOnlyList<TGenotype> genotypes,
        InnerEvaluate innerEvaluate,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem)
    {
        var res = BatchExecution.Parallel(
            repeats,
            r => innerEvaluate(genotypes, r, searchSpace, problem),
            random,
            maxDegreeOfParallelism: maxDegreeOfParallelism);
        var objectiveVectors = new ObjectiveVector[repeats];

        return Enumerable.Range(0, genotypes.Count)
            .Select(genotypeIndex =>
            {
                var genotype = res[0][genotypeIndex].Genotype;
                for (var repetition = 1; repetition < repeats; repetition++)
                {
                    EnsureSameGenotype(genotype, res[repetition][genotypeIndex].Genotype, comparer);
                }

                for (var repetition = 0; repetition < repeats; repetition++)
                {
                    objectiveVectors[repetition] = res[repetition][genotypeIndex].ObjectiveVector;
                }

                return Solution.From(
                    genotype,
                    aggregator(objectiveVectors));
            })
            .ToArray();
    }

    private static ObjectiveVector Mean(ReadOnlySpan<ObjectiveVector> objectiveVectors)
    {
        if (objectiveVectors.Length == 0)
        {
            throw new ArgumentException("At least one objective vector is required.", nameof(objectiveVectors));
        }

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

    private static void EnsureSameGenotype(TGenotype first, TGenotype second, IEqualityComparer<TGenotype> genotypeComparer)
    {
        if (!genotypeComparer.Equals(first, second))
        {
            throw new InvalidOperationException("Repeated evaluator aggregation requires all repeated evaluations of one input slot to return the same genotype.");
        }
    }
}

public static class RepeatingEvaluator
{
    public static RepeatedEvaluator<TGenotype, TSearchSpace, TProblem> AsRepeated<TGenotype, TSearchSpace, TProblem>(
        this IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator,
        int repeats,
        Func<ReadOnlySpan<ObjectiveVector>, ObjectiveVector>? aggregator = null,
        IEqualityComparer<TGenotype>? comparer = null,
        int maxDegreeOfParallelism = -1)
        where TSearchSpace : class, ISearchSpace<TGenotype>
        where TProblem : class, IProblem<TGenotype, TSearchSpace>
        => new(evaluator, repeats, aggregator, comparer, maxDegreeOfParallelism);
}
