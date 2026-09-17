using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

public record CycleAlgorithm<TAlgorithm, TCandidate, TSearchState>
    : Algorithm<CycleAlgorithm<TAlgorithm, TCandidate, TSearchState>, TCandidate, TSearchState>
    where TSearchState : class, ISearchState
    where TAlgorithm : IAlgorithm<TCandidate, TSearchState>
{
    public ValueArray<TAlgorithm> Algorithms { get; }

    public override bool Fits(ExecutionSignature execution) => base.Fits(execution) && execution.Fits([.. Algorithms]);

    /// <summary>
    /// Gets the cycle limit, or <see langword="null"/> for unlimited cycling. The expected value is positive.
    /// </summary>
    /// <remarks>A nonpositive limit runs no cycles.</remarks>
    public int? MaximumCycles { get; init; }

    public bool NewExecutionInstancesPerCycle { get; init; } = true;

    public CycleAlgorithm(IReadOnlyList<TAlgorithm> algorithms)
    {
        if (algorithms.Count == 0)
            throw new ArgumentException("At least one algorithm must be provided.", nameof(algorithms));

        Algorithms = algorithms.ToValueArray();
    }

    public override CycleAlgorithmInstance<TAlgorithm, TCandidate, TRunSearchSpace, TRunProblem, TSearchState> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry) =>
        new(instanceRegistry, Algorithms, MaximumCycles, NewExecutionInstancesPerCycle);
}

public static class CycleAlgorithm
{
    public static CycleAlgorithm<TAlgorithm, TCandidate, TSearchState> Create<TAlgorithm, TCandidate, TSearchState>(
        Algorithm<TAlgorithm, TCandidate, TSearchState> firstAlgorithm, params IReadOnlyList<TAlgorithm> followingAlgorithms)
        where TAlgorithm : Algorithm<TAlgorithm, TCandidate, TSearchState>
        where TSearchState : class, ISearchState => new([firstAlgorithm.Self, .. followingAlgorithms]);

    public static CycleAlgorithm<IAlgorithm<TCandidate, TSearchState>, TCandidate, TSearchState> Create<TCandidate, TSearchState>(
        params IReadOnlyList<IAlgorithm<TCandidate, TSearchState>> algorithms)
        where TSearchState : class, ISearchState => new([.. algorithms]);
}

public static class CycleAlgorithmExtensions
{
    extension<TAlgorithm, TCandidate, TSearchState>(Algorithm<TAlgorithm, TCandidate, TSearchState> algorithm)
        where TAlgorithm : Algorithm<TAlgorithm, TCandidate, TSearchState>
        where TSearchState : class, ISearchState
    {
        public CycleAlgorithm<TAlgorithm, TCandidate, TSearchState> CycleWith(TAlgorithm followingAlgorithm, int? maximumCycles = null) =>
            new([algorithm.Self, followingAlgorithm]) { MaximumCycles = maximumCycles };

        public CycleAlgorithm<TAlgorithm, TCandidate, TSearchState> CycleWith(IReadOnlyList<TAlgorithm> followingAlgorithms, int? maximumCycles = null) =>
            new([algorithm.Self, .. followingAlgorithms]) { MaximumCycles = maximumCycles };
    }

    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        public CycleAlgorithm<IAlgorithm<TCandidate, TSearchState>, TCandidate, TSearchState> CycleWith(
            IAlgorithm<TCandidate, TSearchState> followingAlgorithm, int? maximumCycles = null) =>
            new([algorithm, followingAlgorithm]) { MaximumCycles = maximumCycles };

        public CycleAlgorithm<IAlgorithm<TCandidate, TSearchState>, TCandidate, TSearchState> CycleWith(
            IReadOnlyList<IAlgorithm<TCandidate, TSearchState>> followingAlgorithms, int? maximumCycles = null) =>
            new([algorithm, .. followingAlgorithms]) { MaximumCycles = maximumCycles };
    }
}

public class CycleAlgorithmInstance<TAlgorithm, TCandidate, TSearchSpace, TProblem, TSearchState>
    : AlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
    where TAlgorithm : IAlgorithm<TCandidate, TSearchState>
{
    private readonly ExecutionInstanceRegistry registry;
    protected readonly ImmutableArray<TAlgorithm> Algorithms;
    protected readonly int? MaximumCycles;
    protected readonly bool NewExecutionInstancesPerCycle;

    private readonly Dictionary<IAlgorithm<TCandidate>, IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>> algorithmInstances;

    public CycleAlgorithmInstance(ExecutionInstanceRegistry registry, IReadOnlyList<TAlgorithm> algorithms, int? maximumCycles, bool newExecutionInstancesPerCycle)
    {
        this.registry = registry;
        Algorithms = algorithms.ToImmutableArray();
        MaximumCycles = maximumCycles;
        NewExecutionInstancesPerCycle = newExecutionInstancesPerCycle;

        algorithmInstances = new(capacity: NewExecutionInstancesPerCycle ? 0 : Algorithms.Length, ReferenceEqualityComparer.Instance);
    }

    public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
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
                var algorithmInstance = ResolveAlgorithmInstance(algorithm);

                await foreach (var newState in algorithmInstance.RunStreamingAsync(problem, algorithmRng, state, ct))
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

    private IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> ResolveAlgorithmInstance(TAlgorithm algorithm)
    {
        if (NewExecutionInstancesPerCycle)
        {
            return CreateChildAlgorithmInstance(algorithm);
        }

        if (algorithmInstances.TryGetValue(algorithm, out var existingInstance))
        {
            return existingInstance;
        }

        var newInstance = CreateChildAlgorithmInstance(algorithm);
        algorithmInstances[algorithm] = newInstance;
        return newInstance;
    }

    private IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateChildAlgorithmInstance(TAlgorithm algorithm) =>
        registry.CreateChildRegistry().Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm);
}
