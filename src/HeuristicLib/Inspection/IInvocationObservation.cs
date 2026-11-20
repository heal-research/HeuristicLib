using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>A record of one call of an operation, which also knows how to make an executor report it.</summary>
/// <remarks>
/// Declare it on the record of an operation of your own, as built-in operations do: <c>Wrap</c> returns an executor of the
/// operation that passes each successful call of <c>executor</c> to <c>record</c>. Everything that collects observations,
/// such as <c>Observed</c> and traces, then works for that operation.
/// </remarks>
public interface IInvocationObservation<TOperation, TCandidate, TSelf>
    where TSelf : IInvocationObservation<TOperation, TCandidate, TSelf>
    where TOperation : IOperation<TOperation>
{
    /// <summary>Returns an executor that reports each successful call of the given executor.</summary>
    /// <typeparam name="TSearchState">
    /// The run's search-state type, which resolution supplies. An operation whose executor interface depends on it, such as
    /// termination, casts to that interface with it; other operations ignore it.
    /// </typeparam>
    /// <param name="executor">The executor to wrap, which performs the operation for the run types.</param>
    /// <param name="source">The configuration the observation names.</param>
    /// <param name="record">Receives each observation.</param>
    /// <param name="problem">The problem bound to the executor, also reported in observations.</param>
    static abstract IExecutor Wrap<TSearchState>(
        IExecutor executor, IOperator<TOperation, TCandidate> source, Action<TSelf> record,
        IProblem<TCandidate, ISearchSpace<TCandidate>> problem)
        where TSearchState : class, ISearchState;
}

/// <summary>A record of one thing an algorithm's executor call did, which also knows how to make an executor report it.</summary>
/// <remarks>An algorithm has several boundaries, such as its iterations and its calls, and each has its own record.</remarks>
public interface IAlgorithmObservation<TCandidate, TSearchState, TSelf>
    where TSelf : IAlgorithmObservation<TCandidate, TSearchState, TSelf>
    where TSearchState : class, ISearchState
{
    /// <summary>Whether a counter of this boundary counts this observation.</summary>
    bool Counts => true;

    /// <summary>Returns an executor that reports what the given executor does.</summary>
    /// <param name="executor">The executor to wrap.</param>
    /// <param name="source">The configuration the observation names.</param>
    /// <param name="record">Receives each observation.</param>
    /// <param name="problem">The problem bound to the executor, also reported in observations.</param>
    static abstract IAlgorithmExecutor<TSearchState> Wrap(
        IAlgorithmExecutor<TSearchState> executor, IAlgorithm<TCandidate, TSearchState> source, Action<TSelf> record,
        IProblem<TCandidate, ISearchSpace<TCandidate>> problem);
}
