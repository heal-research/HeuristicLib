using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Partial;

public interface IPartialSolutionProblem<in TGenotype, out TSearchSpace>
    : IProblem<TGenotype, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TGenotype>
{
    IReadOnlyList<bool> IsTerminal(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random);

    IReadOnlyList<ObjectiveVector?> EvaluatePartial(
        IReadOnlyList<TGenotype> genotypes,
        IRandomNumberGenerator random);
}

public static class PartialSolutionProblemExtensions
{
    public static bool IsTerminal<TGenotype, TSearchSpace>(
        this IPartialSolutionProblem<TGenotype, TSearchSpace> problem,
        TGenotype genotype,
        IRandomNumberGenerator random)
        where TSearchSpace : class, ISearchSpace<TGenotype>
        => problem.IsTerminal([genotype], random)[0];

    public static ObjectiveVector? EvaluatePartial<TGenotype, TSearchSpace>(
        this IPartialSolutionProblem<TGenotype, TSearchSpace> problem,
        TGenotype genotype,
        IRandomNumberGenerator random)
        where TSearchSpace : class, ISearchSpace<TGenotype>
        => problem.EvaluatePartial([genotype], random)[0];
}

public interface IBoundedProblem<in TGenotype, out TSearchSpace>
    : IProblem<TGenotype, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TGenotype>
{
    IReadOnlyList<ObjectiveVector> Bound(
        IReadOnlyList<TGenotype> genotypes,
        IRandomNumberGenerator random);
}

public static class BoundedProblemExtensions
{
    public static ObjectiveVector Bound<TGenotype, TSearchSpace>(
        this IBoundedProblem<TGenotype, TSearchSpace> problem,
        TGenotype genotype,
        IRandomNumberGenerator random)
        where TSearchSpace : class, ISearchSpace<TGenotype>
        => problem.Bound([genotype], random)[0];
}

public abstract class SingleSolutionBoundedProblem<TGenotype, TSearchSpace>
    : SingleSolutionProblem<TGenotype, TSearchSpace>,
      IBoundedProblem<TGenotype, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TGenotype>
{
    protected SingleSolutionBoundedProblem(Objective objective, TSearchSpace searchSpace)
        : base(objective, searchSpace)
    { }

    public IReadOnlyList<ObjectiveVector> Bound(
        IReadOnlyList<TGenotype> genotypes,
        IRandomNumberGenerator random)
        => BatchExecution.Parallel(genotypes, Bound, random, DegreeOfParallelism);

    public abstract ObjectiveVector Bound(
        TGenotype genotype,
        IRandomNumberGenerator random);
}

public abstract class SingleSolutionPartialProblem<TGenotype, TSearchSpace>
    : SingleSolutionProblem<TGenotype, TSearchSpace>,
      IPartialSolutionProblem<TGenotype, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TGenotype>
{
    protected SingleSolutionPartialProblem(Objective objective, TSearchSpace searchSpace)
        : base(objective, searchSpace)
    { }

    public IReadOnlyList<bool> IsTerminal(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random) => BatchExecution.Parallel(genotypes, IsTerminal, random, DegreeOfParallelism);

    public IReadOnlyList<ObjectiveVector?> EvaluatePartial(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random) => BatchExecution.Parallel(genotypes, EvaluatePartial, random, DegreeOfParallelism);

    public abstract bool IsTerminal(TGenotype genotype, IRandomNumberGenerator random);

    public abstract ObjectiveVector? EvaluatePartial(TGenotype genotype, IRandomNumberGenerator random);
}

public abstract class SingleSolutionPartialBoundedProblem<TGenotype, TSearchSpace>
    : SingleSolutionPartialProblem<TGenotype, TSearchSpace>,
      IBoundedProblem<TGenotype, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TGenotype>
{
    protected SingleSolutionPartialBoundedProblem(Objective objective, TSearchSpace searchSpace)
        : base(objective, searchSpace)
    { }

    public IReadOnlyList<ObjectiveVector> Bound(
        IReadOnlyList<TGenotype> genotypes,
        IRandomNumberGenerator random)
        => BatchExecution.Parallel(genotypes, Bound, random, DegreeOfParallelism);

    public abstract ObjectiveVector Bound(
        TGenotype genotype,
        IRandomNumberGenerator random);
}
