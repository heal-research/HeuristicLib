using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public record IteratedEvaluator<TGenotype, TSearchSpace, TProblem>
    : WrappingEvaluator<TGenotype, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
    private readonly int iterations;

    public IteratedEvaluator(IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator, int iterations)
        : base(evaluator)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterations);
        this.iterations = iterations;
    }

    protected override IReadOnlyList<Solution<TGenotype>> Evaluate(
        IReadOnlyList<TGenotype> genotypes,
        InnerEvaluate innerEvaluate,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem)
    {
        var currentGenotypes = genotypes;
        IReadOnlyList<Solution<TGenotype>> solutions = [];

        for (var i = 0; i < iterations; i++)
        {
            solutions = innerEvaluate(currentGenotypes, random.Fork(i), searchSpace, problem);
            currentGenotypes = solutions.Select(solution => solution.Genotype).ToArray();
        }

        return solutions;
    }
}

public static class IteratedEvaluator
{
    public static IteratedEvaluator<TGenotype, TSearchSpace, TProblem> AsIterated<TGenotype, TSearchSpace, TProblem>(
        this IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator,
        int iterations)
        where TSearchSpace : class, ISearchSpace<TGenotype>
        where TProblem : class, IProblem<TGenotype, TSearchSpace>
        => new(evaluator, iterations);
}
