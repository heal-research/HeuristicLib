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

    public override StateTerminatedAlgorithmExecution<TCandidate, TRunSearchSpace, TRunProblem, TSearchState> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ResolutionScope scope)
    {
        var typed = scope.For<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>();
        // Resolve the terminator before the wrapped algorithm so elapsed-time terminators start at the earliest point this wrapper controls, including wrapped algorithm instancing.
        var terminator = typed.Resolve(Terminator);
        return new(typed.Resolve(Algorithm), terminator);
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

public class StateTerminatedAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TSearchState> : AlgorithmExecution<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    protected readonly IAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TSearchState> Algorithm;
    protected readonly ITerminatorExecution<TCandidate, TSearchSpace, TProblem, TSearchState> Terminator;

    public StateTerminatedAlgorithmExecution(IAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm, ITerminatorExecution<TCandidate, TSearchSpace, TProblem, TSearchState> terminator)
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
