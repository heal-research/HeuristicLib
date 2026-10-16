using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

/// <remarks>
/// Derive from this base when the algorithm only hands its search space and problem to its operators. Derive from
/// <see cref="Algorithm{TSelf, TCandidate, TSearchSpace, TProblem, TSearchState}"/> when it reads them itself.
/// </remarks>
public abstract record Algorithm<TSelf, TCandidate, TSearchState>
    : IAlgorithm<TCandidate, TSearchState>
    where TSelf : Algorithm<TSelf, TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    internal TSelf Self => (TSelf)this;

    public abstract ExecutorFactory<IAlgorithmExecutor<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>> CreateExecutorFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;

    /// <summary>
    /// Agnostic in the search space and problem, so this base answers on the search state alone, which
    /// <see cref="IAlgorithmExecutor{TCandidate, TSearchSpace, TProblem, TSearchState}"/> is invariant in.
    /// </summary>
    public virtual bool Fits(ExecutionSignature execution) => execution.SearchState == typeof(TSearchState);

    ExecutorFactory<IAlgorithmExecutor<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>> IAlgorithm<TCandidate>.CreateExecutorFactory<TRunSearchSpace, TRunProblem, TRunSearchState>()
    {
        if (typeof(TRunSearchState) != typeof(TSearchState))
        {
            throw new InvalidOperationException($"{GetType().Name} produces {typeof(TSearchState).Name}, and cannot run producing {typeof(TRunSearchState).Name}.");
        }

        return (ExecutorFactory<IAlgorithmExecutor<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>>)CreateExecutorFactory<TRunSearchSpace, TRunProblem>();
    }
}

/// <remarks>
/// Derive from this base only when the algorithm reads its search space or problem itself; naming them costs two type
/// arguments on every mention of the configuration. An algorithm that just hands them to its operators belongs on
/// <see cref="Algorithm{TSelf, TCandidate, TSearchState}"/>.
/// <para>
/// A run this algorithm was not written for is reported when the execution graph is built.
/// </para>
/// </remarks>
public abstract record Algorithm<TSelf, TCandidate, TSearchSpace, TProblem, TSearchState>
    : Algorithm<TSelf, TCandidate, TSearchState>
    where TSelf : Algorithm<TSelf, TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public abstract ExecutorFactory<AlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>> CreateExecutorFactory();

    public override bool Fits(ExecutionSignature execution) =>
        base.Fits(execution)
        && execution.SearchSpace.IsAssignableTo(typeof(TSearchSpace))
        && execution.Problem.IsAssignableTo(typeof(TProblem));

    public sealed override ExecutorFactory<IAlgorithmExecutor<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>> CreateExecutorFactory<TRunSearchSpace, TRunProblem>()
    {
        if (!typeof(TRunSearchSpace).IsAssignableTo(typeof(TSearchSpace)) || !typeof(TRunProblem).IsAssignableTo(typeof(TProblem)))
        {
            throw ExecutionSignature.Mismatch(
                this,
                ExecutionSignature.Describe(typeof(TSearchSpace), typeof(TProblem), typeof(TSearchState)),
                ExecutionSignature.Describe(typeof(TRunSearchSpace), typeof(TRunProblem)));
        }

        return (ExecutorFactory<IAlgorithmExecutor<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>>)CreateExecutorFactory();
    }
}

public abstract class AlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    : IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public abstract IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default);
}
