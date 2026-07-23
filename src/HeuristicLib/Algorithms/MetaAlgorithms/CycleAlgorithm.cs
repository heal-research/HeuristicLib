using System.Runtime.CompilerServices;
using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.MetaAlgorithms;

// ToDo: maybe we need another base class for MetaAlgorithms like this?
// ToDo: think if we want the CycleAlgorithm to terminate internally by checking each result of the inner algorihtms
[Equatable]
public partial record CycleAlgorithm<TAlgorithm, TCandidate, TSearchSpace, TProblem, TSearchState>
    : Algorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
    where TAlgorithm : IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
{
    [OrderedEquality]
    public ImmutableArray<TAlgorithm> Algorithms { get; }

    // ToDo: think if better place outside and keep CycleAlgorithm as infinite cycles?
    public int? MaximumCycles
    {
        get;
        init => field = value is null or > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(MaximumCycles), "MaximumCycles must be positive when set.");
    }

    // ToDo: maybe execution-instance reuse needs a clearer lifecycle concept if this comes up more often.
    public bool NewExecutionInstancesPerCycle { get; init; } = true;

    public CycleAlgorithm(ImmutableArray<TAlgorithm> algorithms)
    {
        if (algorithms.Length == 0)
        {
            throw new ArgumentException("At least one algorithm must be provided.", nameof(algorithms));
        }

        Algorithms = algorithms;
    }

    protected override CycleAlgorithmInstance<TAlgorithm, TCandidate, TSearchSpace, TProblem, TSearchState> CreateAlgorithmInstance(ExecutionInstanceRegistry registry) =>
        new(registry, Algorithms, MaximumCycles, NewExecutionInstancesPerCycle);
}

public class CycleAlgorithmInstance<TAlgorithm, TCandidate, TSearchSpace, TProblem, TSearchState>
    : AlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
    where TAlgorithm : IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
{
    private readonly ExecutionInstanceRegistry registry;
    protected readonly IReadOnlyList<TAlgorithm> Algorithms;
    protected readonly int? MaximumCycles;
    protected readonly bool NewExecutionInstancesPerCycle;

    private readonly Dictionary<IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>, IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>> algorithmInstances;

    public CycleAlgorithmInstance(ExecutionInstanceRegistry registry, IReadOnlyList<TAlgorithm> algorithms, int? maximumCycles, bool newExecutionInstancesPerCycle)
    {
        this.registry = registry;
        Algorithms = algorithms;
        MaximumCycles = maximumCycles;
        NewExecutionInstancesPerCycle = newExecutionInstancesPerCycle;

        algorithmInstances = new(capacity: NewExecutionInstancesPerCycle ? 0 : Algorithms.Count, ReferenceEqualityComparer.Instance);
    }

    public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var state = initialState;

        var cycleCountGenerator = MaximumCycles.HasValue
          ? Enumerable.Range(0, MaximumCycles.Value)
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
        algorithm.CreateExecutionInstance(registry.CreateChildRegistry());
}
