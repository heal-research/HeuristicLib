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
/// Wrapping a contender rather than a meta-algorithm such as racing makes each of the contender's iterations an end of
/// an iteration.
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

    protected override IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope) =>
        new Executor<TSearchSpace, TProblem>(scope.Resolve(ChildAlgorithm));

    private sealed class Executor<TSearchSpace, TProblem>(IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> childAlgorithm)
        : WrappingAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(childAlgorithm)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random,
            TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            // Supports admitted only problem types that implement it.
            var updates = (IIterationUpdatable)problem;
            await foreach (var searchState in ChildAlgorithm.RunStreamingAsync(problem, random, initialState, ct))
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
