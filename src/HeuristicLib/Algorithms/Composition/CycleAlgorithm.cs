using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

/// <remarks>
/// Every turn resolves its algorithm in a new child scope, so private executor data such as terminator counts restarts
/// each turn. Share a configuration through <c>Sharing</c> on the Cycle to keep one executor for it across turns.
/// </remarks>
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

    /// <summary>The observations of the start of each cycle.</summary>
    public ObservationPoint<CycleStart<TCandidate, TSearchState>> Cycles =>
        ObservationPoint.Of<CycleStart<TCandidate, TSearchState>>(this, nameof(Cycles));

    /// <summary>The observations of the start of each turn of an inner algorithm.</summary>
    public ObservationPoint<CycleTurn<TCandidate, TSearchState>> Turns =>
        ObservationPoint.Of<CycleTurn<TCandidate, TSearchState>>(this, nameof(Turns));

    public CycleAlgorithm(IReadOnlyList<TAlgorithm> algorithms)
    {
        if (algorithms.Count == 0)
            throw new ArgumentException("At least one algorithm must be provided.", nameof(algorithms));

        Algorithms = algorithms.ToValueArray();
    }

    /// <remarks>
    /// The executor publishes the start of each cycle and turn to the observers of <c>Cycles</c> and <c>Turns</c>.
    /// </remarks>
    protected override IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        var cycles = scope.Resolve(Cycles);
        var turns = scope.Resolve(Turns);
        return new CycleAlgorithmExecutor<TAlgorithm, TCandidate, TSearchSpace, TProblem, TSearchState>(
            scope, Algorithms, MaximumCycles, cycles, turns);
    }
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

public class CycleAlgorithmExecutor<TAlgorithm, TCandidate, TSearchSpace, TProblem, TSearchState>
    : AlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
    where TAlgorithm : IAlgorithm<TCandidate, TSearchState>
{
    private readonly ResolutionScope scope;
    protected readonly ImmutableArray<TAlgorithm> Algorithms;
    protected readonly int? MaximumCycles;
    private readonly IObservationPointExecutor<CycleStart<TCandidate, TSearchState>> cycles;
    private readonly IObservationPointExecutor<CycleTurn<TCandidate, TSearchState>> turns;

    public CycleAlgorithmExecutor(ResolutionScope scope, IReadOnlyList<TAlgorithm> algorithms, int? maximumCycles,
        IObservationPointExecutor<CycleStart<TCandidate, TSearchState>> cycles,
        IObservationPointExecutor<CycleTurn<TCandidate, TSearchState>> turns)
    {
        this.scope = scope;
        Algorithms = algorithms.ToImmutableArray();
        MaximumCycles = maximumCycles;
        this.cycles = cycles;
        this.turns = turns;
    }

    public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var state = initialState;
        var turn = 0L;

        var cycleCountGenerator = MaximumCycles.HasValue
            ? Enumerable.Range(0, Math.Max(0, MaximumCycles.Value))
            : Enumerable.InfiniteSequence(0, 1);

        foreach (var cycleCount in cycleCountGenerator)
        {
            ct.ThrowIfCancellationRequested();
            var producedState = false;
            var cycleRng = random.Fork(cycleCount);
            cycles.Report(new(cycleCount + 1, state, problem.SearchSpace, problem));
            foreach (var (algorithm, algorithmIndex) in Algorithms.Select((a, i) => (a, i)))
            {
                ct.ThrowIfCancellationRequested();
                var algorithmRng = cycleRng.Fork(algorithmIndex);
                var algorithmExecutor = scope.CreateChildScope().Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm);
                turns.Report(new(cycleCount + 1, ++turn, algorithm, algorithmIndex, state, problem.SearchSpace, problem));

                await foreach (var newState in algorithmExecutor.RunStreamingAsync(problem, algorithmRng, state, ct))
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
