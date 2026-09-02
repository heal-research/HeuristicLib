using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

/// <remarks>
/// A wrapping crossover owns a child, so it stays agnostic in the search space and problem and passes the run's triple
/// through unchanged. Binding is a leaf concept: a composite that narrowed the triple would reject children the run
/// supports.
/// </remarks>
public abstract record WrappingCrossover<TCandidate>
    : ICrossover<TCandidate>
{
    protected WrappingCrossover(ICrossover<TCandidate> childCrossover)
    {
        ChildCrossover = childCrossover;
    }

    public ICrossover<TCandidate> ChildCrossover { get; init; }

    /// <summary>
    /// Resolves the child over the run's search space and problem and hands it to
    /// <see cref="WrapExecutionInstance{TRunSearchSpace, TRunProblem}"/>. Left visible, because unlike a leaf
    /// crossover this base offers no other creation member and hiding it would leave an author with no view of the
    /// mechanism their override plugs into.
    /// </summary>
    public ICrossoverInstance<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace> =>
        WrapExecutionInstance(instanceRegistry.Resolve<TCandidate, TRunSearchSpace, TRunProblem>(ChildCrossover));

    /// <summary>Wraps the child's execution instance in this operator's own.</summary>
    protected abstract ICrossoverInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(ICrossoverInstance<TCandidate, TRunSearchSpace, TRunProblem> childCrossover)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public abstract class WrappingCrossoverInstance<TCandidate, TSearchSpace, TProblem>(ICrossoverInstance<TCandidate, TSearchSpace, TProblem> childCrossover)
    : CrossoverInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ICrossoverInstance<TCandidate, TSearchSpace, TProblem> ChildCrossover { get; } = childCrossover;
}
