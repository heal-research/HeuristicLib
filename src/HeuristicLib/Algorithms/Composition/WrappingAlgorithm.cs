using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

public abstract record WrappingAlgorithm<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> ChildAlgorithm)
    : IAlgorithm<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    public virtual bool Fits(ExecutionSignature execution) => execution.SearchState == typeof(TSearchState) && execution.Fits(ChildAlgorithm);

    public ExecutionFactory<IAlgorithmExecution<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>> CreateExecutionFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        var advice = CreateWrapperFactory<TRunSearchSpace, TRunProblem>();
        return scope => advice(scope.Resolve<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>(ChildAlgorithm));
    }

    protected abstract WrapperExecutionFactory<IAlgorithmExecution<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;

    ExecutionFactory<IAlgorithmExecution<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>> IAlgorithm<TCandidate>.CreateExecutionFactory<TRunSearchSpace, TRunProblem, TRunSearchState>()
    {
        if (typeof(TRunSearchState) != typeof(TSearchState))
            throw new InvalidOperationException($"{GetType().Name} produces {typeof(TSearchState).Name}, and cannot run producing {typeof(TRunSearchState).Name}.");
        return (ExecutionFactory<IAlgorithmExecution<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>>)CreateExecutionFactory<TRunSearchSpace, TRunProblem>();
    }
}

public abstract class WrappingAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TSearchState>(
    IAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TSearchState> childAlgorithm)
    : AlgorithmExecution<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    protected IAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TSearchState> ChildAlgorithm { get; } = childAlgorithm;
}
