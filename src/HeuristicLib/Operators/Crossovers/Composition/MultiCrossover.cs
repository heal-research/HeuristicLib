using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

/// <remarks>
/// A multi crossover owns children, so it stays agnostic in the search space and problem and passes the run's triple
/// through unchanged.
/// </remarks>
public abstract record MultiCrossover<TCandidate>
    : ICrossover<TCandidate>
{
    protected MultiCrossover(IReadOnlyList<ICrossover<TCandidate>> childCrossovers)
    {
        ChildCrossovers = childCrossovers.ToValueArray();
    }

    public ValueArray<ICrossover<TCandidate>> ChildCrossovers { get; init; }

    public virtual bool Fits(ExecutionSignature execution) => execution.Fits([.. ChildCrossovers]);


    /// <summary>
    /// Resolves each child over the run's search space and problem and hands them to
    /// <see cref="CombineExecutionInstances{TRunSearchSpace, TRunProblem}"/>.
    /// </summary>
    public ICrossoverExecution<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ResolutionScope scope)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        var typed = scope.For<TCandidate, TRunSearchSpace, TRunProblem>();
        return CombineExecutionInstances([.. ChildCrossovers.Select(child => typed.Resolve(child))]);
    }

    /// <summary>Combines the children's execution nodes into this operator's own.</summary>
    protected abstract ICrossoverExecution<TCandidate, TRunSearchSpace, TRunProblem> CombineExecutionInstances<TRunSearchSpace, TRunProblem>(ImmutableArray<ICrossoverExecution<TCandidate, TRunSearchSpace, TRunProblem>> childCrossovers)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public abstract class MultiCrossoverExecution<TCandidate, TSearchSpace, TProblem>(ImmutableArray<ICrossoverExecution<TCandidate, TSearchSpace, TProblem>> childCrossovers)
    : CrossoverExecution<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<ICrossoverExecution<TCandidate, TSearchSpace, TProblem>> ChildCrossovers { get; } = childCrossovers;
}
