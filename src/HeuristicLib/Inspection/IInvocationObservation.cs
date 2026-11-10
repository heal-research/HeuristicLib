using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
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
    /// <param name="executor">The executor to wrap, which performs the operation for the run types.</param>
    /// <param name="source">The configuration the observation names.</param>
    /// <param name="record">Receives each observation.</param>
    static abstract IExecutor Wrap<TSearchSpace, TProblem, TSearchState>(IExecutor executor, IOperator<TOperation, TCandidate> source, Action<TSelf> record)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, Problems.IProblem<TCandidate, TSearchSpace>
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
    static abstract IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> Wrap<TSearchSpace, TProblem>(
        IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> executor, IAlgorithm<TCandidate, TSearchState> source, Action<TSelf> record)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, Problems.IProblem<TCandidate, TSearchSpace>;
}

// Temporary: Aspect.Observe reaches the same wrappers through these until phase 3 removes advice.
internal sealed class ObservationAdvice<TOperation, TCandidate, TObservation>(Action<TObservation> record) : OperatorAdvice<TOperation, TCandidate>
    where TOperation : IOperation<TOperation>
    where TObservation : IInvocationObservation<TOperation, TCandidate, TObservation>
{
    public override IExecutor Wrap<TSearchSpace, TProblem, TSearchState>(IExecutor executor, OperationInfo operation, IOperator<TOperation, TCandidate> source) =>
        TObservation.Wrap<TSearchSpace, TProblem, TSearchState>(executor, source, record);
}

internal sealed class AlgorithmObservationAdvice<TCandidate, TSearchState, TObservation>(Action<TObservation> record) : AlgorithmAdvice<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
    where TObservation : IAlgorithmObservation<TCandidate, TSearchState, TObservation>
{
    public override IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> Wrap<TSearchSpace, TProblem>(
        IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> executor, IAlgorithm<TCandidate, TSearchState> source) =>
        TObservation.Wrap(executor, source, record);
}
