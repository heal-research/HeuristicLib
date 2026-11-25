using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Dynamic;

/// <summary>
/// Ends an iteration of a dynamic problem each time its child algorithm yields a state, so that updates the problem
/// postpones to the end of an iteration are applied there.
/// </summary>
/// <remarks>
/// <para>
/// Only <see cref="UpdatePolicy.AfterEachIteration"/> needs this, because an iteration belongs to an algorithm rather than
/// to the problem; under every other policy the wrapper changes nothing. The problem is the one the wrapped call
/// receives, and it must derive from <see cref="DynamicProblem{TSelf, TCandidate, TSearchSpace}"/>.
/// </para>
/// <para>
/// The updates are applied before the state is yielded, so a caller that pauses after a state already sees the epoch that
/// state's iteration completed, and a stream that is disposed or fails before its next state applies nothing more.
/// Wrapping a contender rather than a meta-algorithm makes each of the contender's iterations an end of an iteration;
/// racing wraps each of its contenders this way itself.
/// </para>
/// </remarks>
public sealed record IterationUpdatingAlgorithm<TCandidate, TSearchState> : WrappingAlgorithm<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    public IterationUpdatingAlgorithm(IAlgorithm<TCandidate, TSearchState> childAlgorithm)
        : base(childAlgorithm)
    {
    }

    public override bool Supports(ExecutionSignature execution) =>
        base.Supports(execution) && execution.Problem.IsAssignableTo(typeof(IIterationUpdatable));

    protected override IAlgorithmExecutor<TSearchState> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        var childAlgorithm = scope.Resolve(ChildAlgorithm);
        // Supports admits only problem types that implement IIterationUpdatable.
        return new Executor(childAlgorithm, (IIterationUpdatable)scope.Problem);
    }

    private sealed class Executor(IAlgorithmExecutor<TSearchState> childAlgorithm, IIterationUpdatable updates)
        : WrappingAlgorithmExecutor<TSearchState>(childAlgorithm)
    {
        public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            await foreach (var searchState in ChildAlgorithm.RunStreamingAsync(random, initialState, ct))
            {
                updates.CompleteIteration();
                yield return searchState;
            }
        }
    }
}

public static class IterationUpdates
{
    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        /// <summary>Applies the updates a dynamic problem postpones to the end of an iteration each time this algorithm yields a state.</summary>
        /// <remarks>The run's problem must derive from <see cref="DynamicProblem{TSelf, TCandidate, TSearchSpace}"/>.</remarks>
        public IterationUpdatingAlgorithm<TCandidate, TSearchState> ApplyingIterationUpdates() => new(algorithm);
    }
}
