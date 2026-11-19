using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

// Adapter for algorithms that do not have an inner termination criterion; revisit if every algorithm exposes a terminal-state module.
public record StateTerminatedAlgorithm<TCandidate, TSearchState>
    : Algorithm<StateTerminatedAlgorithm<TCandidate, TSearchState>, TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    public required IAlgorithm<TCandidate, TSearchState> Algorithm { get; init; }
    public required IOperator<Termination, TCandidate> Terminator { get; init; }

    public static Parameter<StateTerminatedAlgorithm<TCandidate, TSearchState>, IAlgorithm<TCandidate, TSearchState>> AlgorithmParameter { get; } =
        new(nameof(Algorithm), static algorithm => algorithm.Algorithm, static (algorithm, value) => algorithm with { Algorithm = value });

    public static Parameter<StateTerminatedAlgorithm<TCandidate, TSearchState>, IOperator<Termination, TCandidate>> TerminatorParameter { get; } =
        new(nameof(Terminator), static algorithm => algorithm.Terminator, static (algorithm, value) => algorithm with { Terminator = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [AlgorithmParameter, TerminatorParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override IAlgorithmExecutor<TSearchState> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        // Resolve the terminator before the wrapped algorithm so elapsed-time terminators start before wrapped algorithm construction.
        var terminator = scope.Resolve(Terminator);
        var algorithm = scope.Resolve(Algorithm);
        return new Executor(algorithm, terminator);
    }

    private sealed class Executor : AlgorithmExecutor<TSearchState>
    {
        private IAlgorithmExecutor<TSearchState> Algorithm { get; }
        private ITerminationExecutor<TSearchState> Terminator { get; }

        // Iterative algorithms fork the run's generator by iteration index. A key no iteration reaches keeps the terminator
        // from receiving the stream of an inner iteration.
        private const ulong TerminationForkKey = ulong.MaxValue;

        public Executor(IAlgorithmExecutor<TSearchState> algorithm, ITerminationExecutor<TSearchState> terminator)
        {
            Algorithm = algorithm;
            Terminator = terminator;
        }

        public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var terminationRandom = random.Fork(TerminationForkKey);
            var yieldedStateCount = 0;
            await foreach (var state in Algorithm.RunStreamingAsync(random, initialState, ct))
            {
                yield return state;

                if (Terminator.IsTerminalState(state, terminationRandom.Fork(yieldedStateCount)))
                {
                    yield break;
                }

                yieldedStateCount++;
            }
        }
    }
}

public static class StateTerminatedAlgorithm
{
    public static StateTerminatedAlgorithm<TCandidate, TSearchState> Create<TCandidate, TSearchState>(
        IAlgorithm<TCandidate, TSearchState> algorithm, IOperator<Termination, TCandidate> terminator)
        where TSearchState : class, ISearchState => new()
        {
            Algorithm = algorithm,
            Terminator = terminator
        };
}

public static class StateTerminatedAlgorithmExtensions
{
    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        public StateTerminatedAlgorithm<TCandidate, TSearchState> TerminatedBy(IOperator<Termination, TCandidate> terminator)
        {
            return StateTerminatedAlgorithm.Create(algorithm, terminator);
        }

        public StateTerminatedAlgorithm<TCandidate, TSearchState> TerminatedAfterIterations(int maximumIterations)
        {
            return algorithm.TerminatedBy(new AfterIterationsTerminator<TCandidate>(maximumIterations));
        }
    }
}
