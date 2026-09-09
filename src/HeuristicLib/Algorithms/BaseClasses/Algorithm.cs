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

    public abstract IAlgorithmInstance<TCandidate, TRunSearchSpace, TRunProblem, TSearchState> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;

    IAlgorithmInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> IAlgorithm<TCandidate>.CreateExecutionInstance<TRunSearchSpace, TRunProblem, TRunSearchState>(ExecutionInstanceRegistry instanceRegistry)
    {
        if (CreateExecutionInstance<TRunSearchSpace, TRunProblem>(instanceRegistry) is not IAlgorithmInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> instance)
        {
            throw new InvalidOperationException(
                $"{GetType().Name} produces {typeof(TSearchState).Name}, and cannot run producing {typeof(TRunSearchState).Name}.");
        }

        return instance;
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
    public abstract AlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);

    public sealed override IAlgorithmInstance<TCandidate, TRunSearchSpace, TRunProblem, TSearchState> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
    {
        if (CreateExecutionInstance(instanceRegistry) is not IAlgorithmInstance<TCandidate, TRunSearchSpace, TRunProblem, TSearchState> instance)
        {
            throw new InvalidOperationException(
                $"{GetType().Name} is written for {typeof(TSearchSpace).Name} and {typeof(TProblem).Name}, and cannot run over {typeof(TRunSearchSpace).Name} with {typeof(TRunProblem).Name}.");
        }

        return instance;
    }
}

public abstract class AlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    : IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public abstract IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default);
}
