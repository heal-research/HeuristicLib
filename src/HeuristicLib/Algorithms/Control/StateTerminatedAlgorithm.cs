using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

// Adapter for algorithms that do not have an inner termination criterion; revisit if every algorithm exposes a terminal-state hook.
public record StateTerminatedAlgorithm<TCandidate, TSearchState>
    : Algorithm<StateTerminatedAlgorithm<TCandidate, TSearchState>, TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    public required IAlgorithm<TCandidate, TSearchState> Algorithm { get; init; }
    public required ITerminator<TCandidate> Terminator { get; init; }

    public override StateTerminatedAlgorithmInstance<TCandidate, TRunSearchSpace, TRunProblem, TSearchState> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
    {
        var resolver = instanceRegistry.For<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>();
        // Resolve the terminator before the wrapped algorithm so elapsed-time terminators start at the earliest point this wrapper controls, including wrapped algorithm instancing.
        var terminator = resolver.Resolve(Terminator);
        return new(resolver.Resolve(Algorithm), terminator);
    }
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

public class StateTerminatedAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> : AlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    protected readonly IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> Algorithm;
    protected readonly ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> Terminator;

    public StateTerminatedAlgorithmInstance(IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm, ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> terminator)
    {
        Algorithm = algorithm;
        Terminator = terminator;
    }

    public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var state in Algorithm.RunStreamingAsync(problem, random, initialState, ct))
        {
            yield return state;

            if (Terminator.IsTerminalState(state, problem.SearchSpace, problem))
            {
                yield break;
            }
        }
    }
}

public static class StateTerminatedAlgorithmExtensions
{
    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        public StateTerminatedAlgorithm<TCandidate, TSearchState> WithTerminator(ITerminator<TCandidate> terminator)
        {
            return StateTerminatedAlgorithm.Create(algorithm, terminator);
        }

        public StateTerminatedAlgorithm<TCandidate, TSearchState> WithMaxIterations(int maximumIterations)
        {
            return algorithm.WithTerminator(new AfterIterationsTerminator<TCandidate>(maximumIterations));
        }
    }
}
