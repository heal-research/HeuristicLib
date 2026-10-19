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
    public required ITerminator<TCandidate> Terminator { get; init; }

    public override bool Fits(ExecutionSignature execution) => base.Fits(execution) && execution.Fits(Algorithm, Terminator);

    public override ExecutorFactory<IAlgorithmExecutor<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>> CreateExecutorFactory<TRunSearchSpace, TRunProblem>() => scope =>
    {
        var typed = scope.For<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>();
        // Resolve the terminator before the wrapped algorithm so elapsed-time terminators start before wrapped algorithm construction.
        var terminator = typed.Resolve(Terminator);
        return new StateTerminatedAlgorithmExecutor<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>(typed.Resolve(Algorithm), terminator);
    };
}

public static class StateTerminatedAlgorithm
{
    public static StateTerminatedAlgorithm<TCandidate, TSearchState> Create<TCandidate, TSearchState>(
        IAlgorithm<TCandidate, TSearchState> algorithm, ITerminator<TCandidate> terminator)
        where TSearchState : class, ISearchState => new()
        {
            Algorithm = algorithm,
            Terminator = terminator
        };
}

public class StateTerminatedAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> : AlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    protected readonly IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> Algorithm;
    protected readonly ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> Terminator;

    public StateTerminatedAlgorithmExecutor(IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm, ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> terminator)
    {
        Algorithm = algorithm;
        Terminator = terminator;
    }

    // Iterative algorithms fork the run's generator by iteration index. A key no iteration reaches keeps the terminator
    // from receiving the stream of an inner iteration.
    private const ulong TerminationForkKey = ulong.MaxValue;

    public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var terminationRandom = random.Fork(TerminationForkKey);
        var yieldedStateCount = 0;
        await foreach (var state in Algorithm.RunStreamingAsync(problem, random, initialState, ct))
        {
            yield return state;

            if (Terminator.IsTerminalState(state, terminationRandom.Fork(yieldedStateCount), problem.SearchSpace, problem))
            {
                yield break;
            }

            yieldedStateCount++;
        }
    }
}

public static class StateTerminatedAlgorithmExtensions
{
    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        public StateTerminatedAlgorithm<TCandidate, TSearchState> TerminatedBy(ITerminator<TCandidate> terminator)
        {
            return StateTerminatedAlgorithm.Create(algorithm, terminator);
        }

        public StateTerminatedAlgorithm<TCandidate, TSearchState> TerminatedAfterIterations(int maximumIterations)
        {
            return algorithm.TerminatedBy(new AfterIterationsTerminator<TCandidate>(maximumIterations));
        }
    }
}
