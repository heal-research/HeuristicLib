using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

/// <remarks>
/// Every turn resolves its algorithm in a new child scope, so private executor data such as terminator counts restarts
/// each turn. Share a configuration through <c>Sharing</c> on the Cycle to keep one executor for it across turns.
/// </remarks>
public record CycleAlgorithm<TCandidate, TSearchState>
    : Algorithm<CycleAlgorithm<TCandidate, TSearchState>, TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    public ValueArray<IAlgorithm<TCandidate, TSearchState>> Algorithms { get; init; }

    public static ListParameter<CycleAlgorithm<TCandidate, TSearchState>, IAlgorithm<TCandidate, TSearchState>> AlgorithmsParameter { get; } =
        new(nameof(Algorithms), static cycle => cycle.Algorithms, static (cycle, value) => cycle with { Algorithms = value.ToValueArray() });

    public static Parameter<CycleAlgorithm<TCandidate, TSearchState>, int?> MaximumCyclesParameter { get; } =
        new(nameof(MaximumCycles), static cycle => cycle.MaximumCycles, static (cycle, value) => cycle with { MaximumCycles = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [AlgorithmsParameter, MaximumCyclesParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    /// <summary>
    /// Gets the cycle limit, or <see langword="null"/> for unlimited cycling. The expected value is positive.
    /// </summary>
    /// <remarks>A nonpositive limit runs no cycles.</remarks>
    public int? MaximumCycles { get; init; }

    public CycleAlgorithm(IReadOnlyList<IAlgorithm<TCandidate, TSearchState>> algorithms)
    {
        if (algorithms.Count == 0)
            throw new ArgumentException("At least one algorithm must be provided.", nameof(algorithms));

        Algorithms = algorithms.ToValueArray();
    }

    protected override IAlgorithmExecutor<TSearchState> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        if (Algorithms.Count == 0)
            throw new InvalidOperationException("A cycle needs at least one algorithm.");
        return new Executor<TSearchSpace, TProblem>(scope, Algorithms, MaximumCycles, scope.Problem);
    }

    private sealed class Executor<TSearchSpace, TProblem>
        : AlgorithmExecutor<TSearchState>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        private readonly ResolutionScope scope;
        private readonly TProblem problem;
        private ImmutableArray<IAlgorithm<TCandidate, TSearchState>> Algorithms { get; }
        private int? MaximumCycles { get; }

        public Executor(ResolutionScope scope, IReadOnlyList<IAlgorithm<TCandidate, TSearchState>> algorithms, int? maximumCycles, TProblem problem)
        {
            this.scope = scope;
            this.problem = problem;
            Algorithms = algorithms.ToImmutableArray();
            MaximumCycles = maximumCycles;
        }

        public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var state = initialState;

            var cycleCountGenerator = MaximumCycles.HasValue
                ? Enumerable.Range(0, Math.Max(0, MaximumCycles.Value))
                : Enumerable.InfiniteSequence(0, 1);

            foreach (var cycleCount in cycleCountGenerator)
            {
                ct.ThrowIfCancellationRequested();
                var producedState = false;
                var cycleRng = random.Fork(cycleCount);
                foreach (var (algorithm, algorithmIndex) in Algorithms.Select((a, i) => (a, i)))
                {
                    ct.ThrowIfCancellationRequested();
                    var algorithmRng = cycleRng.Fork(algorithmIndex);
                    var algorithmExecutor = scope.CreateChildScope().Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm, problem);

                    await foreach (var newState in algorithmExecutor.RunStreamingAsync(algorithmRng, state, ct))
                    {
                        producedState = true;
                        state = newState;
                        yield return newState;
                    }
                }

                if (!producedState)
                {
                    await Task.Yield();
                }
            }
        }
    }
}

public static class CycleAlgorithm
{
    public static CycleAlgorithm<TCandidate, TSearchState> Create<TCandidate, TSearchState>(
        params IReadOnlyList<IAlgorithm<TCandidate, TSearchState>> algorithms)
        where TSearchState : class, ISearchState => new([.. algorithms]);
}

public static class CycleAlgorithmExtensions
{
    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        public CycleAlgorithm<TCandidate, TSearchState> CycleWith(
            IAlgorithm<TCandidate, TSearchState> followingAlgorithm, int? maximumCycles = null) =>
            new([algorithm, followingAlgorithm]) { MaximumCycles = maximumCycles };

        public CycleAlgorithm<TCandidate, TSearchState> CycleWith(
            IReadOnlyList<IAlgorithm<TCandidate, TSearchState>> followingAlgorithms, int? maximumCycles = null) =>
            new([algorithm, .. followingAlgorithms]) { MaximumCycles = maximumCycles };
    }
}
