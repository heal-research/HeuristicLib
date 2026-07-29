using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public abstract record Algorithm<TSelf, TCandidate, TSearchSpace, TProblem, TSearchState>
    : IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSelf : Algorithm<TSelf, TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    internal TSelf Self => (TSelf)this;

    protected abstract AlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateAlgorithmInstance(ExecutionInstanceRegistry registry);

    IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> IExecutionInstanceResolvable<IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>>.CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateAlgorithmInstance(instanceRegistry);
}

public abstract class AlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    : IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public abstract IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default);
}

public static class AlgorithmExtensions
{
    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(
        IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public AlgorithmRun<TCandidate, TSearchSpace, TProblem, TSearchState> CreateRun(TProblem problem, IRandomNumberGenerator random)
        {
            return new(algorithm, problem, random);
        }

        public ExecutionStream<TSearchState> Stream(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default)
        {
            return algorithm.CreateRun(problem, random).Stream(initialState, ct);
        }

        public async Task<TSearchState> CompleteAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default)
        {
            return await algorithm.CreateRun(problem, random).CompleteAsync(initialState, ct);
        }

        public TSearchState Complete(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default)
        {
            return algorithm.CreateRun(problem, random).Complete(initialState, ct);
        }
    }

    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(
        IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> algorithmInstance)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public async Task<TSearchState> CompleteAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default)
        {
            return await algorithmInstance.RunStreamingAsync(problem, random, initialState, ct).LastAsync(ct);
        }

        public IEnumerable<TSearchState> Stream(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default)
        {
            return algorithmInstance.RunStreamingAsync(problem, random, initialState, ct).ToBlockingEnumerable(ct);
        }

        public TSearchState Complete(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default)
        {
            return algorithmInstance.CompleteAsync(problem, random, initialState, ct).GetAwaiter().GetResult();
        }
    }
}
