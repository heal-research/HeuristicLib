using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

public abstract record WrappingAlgorithm<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> ChildAlgorithm)
    : IAlgorithm<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    public virtual bool Fits(ExecutionSignature execution) => execution.SearchState == typeof(TSearchState) && execution.Fits(ChildAlgorithm);

    public ExecutorFactory<IAlgorithmExecutor<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>> CreateExecutorFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        var advice = CreateWrapperFactory<TRunSearchSpace, TRunProblem>();
        return scope => advice(scope.Resolve<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>(ChildAlgorithm));
    }

    protected abstract WrapperExecutorFactory<IAlgorithmExecutor<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;

    ExecutorFactory<IAlgorithmExecutor<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>> IAlgorithm<TCandidate>.CreateExecutorFactory<TRunSearchSpace, TRunProblem, TRunSearchState>()
    {
        if (typeof(TRunSearchState) != typeof(TSearchState))
            throw new InvalidOperationException($"{GetType().Name} produces {typeof(TSearchState).Name}, and cannot run producing {typeof(TRunSearchState).Name}.");
        return (ExecutorFactory<IAlgorithmExecutor<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>>)CreateExecutorFactory<TRunSearchSpace, TRunProblem>();
    }
}

public abstract class WrappingAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(
    IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> childAlgorithm)
    : AlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    protected IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> ChildAlgorithm { get; } = childAlgorithm;
}
