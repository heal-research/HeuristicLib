using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

/// <remarks>
/// A multi crossover owns children, so it stays agnostic in the search space and problem and passes the run's triple
/// through unchanged. See <see cref="WrappingCrossover{TCandidate}"/> for why binding is a leaf concept.
/// </remarks>
public abstract record MultiCrossover<TCandidate>
    : ICrossover<TCandidate>
{
    protected MultiCrossover(IReadOnlyList<ICrossover<TCandidate>> childCrossovers)
    {
        ChildCrossovers = childCrossovers.ToValueArray();
    }

    public ValueArray<ICrossover<TCandidate>> ChildCrossovers { get; init; }

    /// <summary>
    /// Resolves each child over the run's search space and problem and hands them to
    /// <see cref="CombineExecutionInstances{TRunSearchSpace, TRunProblem}"/>. Left visible, because unlike a leaf
    /// crossover this base offers no other creation member and hiding it would leave an author with no view of the
    /// mechanism their override plugs into.
    /// </summary>
    public ICrossoverInstance<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        var resolver = instanceRegistry.For<TCandidate, TRunSearchSpace, TRunProblem>();
        return CombineExecutionInstances([.. ChildCrossovers.Select(child => resolver.Resolve(child))]);
    }

    /// <summary>Combines the children's execution instances into this operator's own.</summary>
    protected abstract ICrossoverInstance<TCandidate, TRunSearchSpace, TRunProblem> CombineExecutionInstances<TRunSearchSpace, TRunProblem>(ImmutableArray<ICrossoverInstance<TCandidate, TRunSearchSpace, TRunProblem>> childCrossovers)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public abstract class MultiCrossoverInstance<TCandidate, TSearchSpace, TProblem>(ImmutableArray<ICrossoverInstance<TCandidate, TSearchSpace, TProblem>> childCrossovers)
    : CrossoverInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<ICrossoverInstance<TCandidate, TSearchSpace, TProblem>> ChildCrossovers { get; } = childCrossovers;
}
