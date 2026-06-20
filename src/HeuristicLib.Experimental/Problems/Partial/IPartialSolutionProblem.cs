using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Partial;

public interface IPartialSolutionProblem<in TGenotype, out TSearchSpace>
    : IProblem<TGenotype, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TGenotype>
{
    bool IsTerminal(TGenotype genotype, IRandomNumberGenerator random);

    ObjectiveVector? EvaluatePartial(TGenotype genotype, IRandomNumberGenerator random);
}

public interface IBoundedProblem<in TGenotype, out TSearchSpace>
    : IProblem<TGenotype, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TGenotype>
{
    ObjectiveVector Bound(
        TGenotype genotype,
        IRandomNumberGenerator random);
}

public abstract class SingleSolutionBoundedProblem<TGenotype, TSearchSpace>
    : SingleSolutionProblem<TGenotype, TSearchSpace>,
      IBoundedProblem<TGenotype, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TGenotype>
{
    protected SingleSolutionBoundedProblem(Objective objective, TSearchSpace searchSpace)
        : base(objective, searchSpace)
    { }

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

    public abstract ObjectiveVector Bound(
        TGenotype genotype,
        IRandomNumberGenerator random);
}
