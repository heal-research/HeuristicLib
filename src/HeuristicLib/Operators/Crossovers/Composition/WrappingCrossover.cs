using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

/// <remarks>
/// A wrapping crossover owns a child, so it stays agnostic in the search space and problem and passes the run's triple
/// through unchanged.
/// </remarks>
public abstract record WrappingCrossover<TCandidate>
    : ICrossover<TCandidate>
{
    protected WrappingCrossover(ICrossover<TCandidate> childCrossover)
    {
        ChildCrossover = childCrossover;
    }

    public ICrossover<TCandidate> ChildCrossover { get; init; }

    public virtual bool Fits(ExecutionSignature execution) => execution.Fits(ChildCrossover);

    /// <summary>Prepares this operator once, then binds its child in each construction scope.</summary>
    public ExecutorFactory<ICrossoverExecutor<TCandidate, TRunSearchSpace, TRunProblem>> CreateExecutorFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        var create = CreateWrapperFactory<TRunSearchSpace, TRunProblem>();
        return scope => create(scope.Resolve<TCandidate, TRunSearchSpace, TRunProblem>(ChildCrossover));
    }

    /// <summary>Prepares persistent execution data and returns a constructor accepting the resolved child.</summary>
    /// <remarks>Allocate shared state here; construct nodes with their contextual children in the returned delegate.</remarks>
    protected abstract WrapperExecutorFactory<ICrossoverExecutor<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;

}

public abstract class WrappingCrossoverExecutor<TCandidate, TSearchSpace, TProblem>(ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> childCrossover)
    : CrossoverExecutor<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> ChildCrossover { get; } = childCrossover;
}
