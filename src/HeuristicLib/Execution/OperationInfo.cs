using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Execution;

/// <summary>Describes one operation for the run types of one resolution, such as mutation over one search space and problem.</summary>
/// <remarks>
/// Resolution passes this to advice, which checks and wraps executors without knowing their operation. The operation's
/// own <see cref="IOperation{TSelf}"/> members supply every answer.
/// </remarks>
public sealed class OperationInfo
{
    private readonly Members members;

    private OperationInfo(bool supportsCandidateWork, Members members)
    {
        SupportsCandidateWork = supportsCandidateWork;
        this.members = members;
    }

    /// <summary>Whether the operation's invocations report candidate work.</summary>
    /// <remarks>Advice measuring candidate work checks this before the executor is built.</remarks>
    public bool SupportsCandidateWork { get; }

    /// <summary>Rejects an operator whose operation reports no candidate work.</summary>
    public void RequireCandidateWork(IOperator source, string metric) => members.RequireCandidateWork(source, metric);

    // Rejects an executor that does not implement the operation's executor interface for these run types.
    internal void RequireExecutor(IExecutor executor, object origin) => members.RequireExecutor(executor, origin);

    /// <summary>Returns an executor whose named method passes each call, as an invocation, to the wrapper.</summary>
    /// <remarks>
    /// The returned executor forwards the controls of the executor it wraps, unless the wrapper implements
    /// <see cref="IExecutorControlProvider"/> and supplies them.
    /// </remarks>
    public IExecutor Wrap<TWrapper>(IExecutor executor, TWrapper wrapper) where TWrapper : struct, IInvocationWrapper =>
        members.Wrap(executor, wrapper);

    internal static OperationInfo For<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState>()
        where TOperation : IOperation<TOperation>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        Cache<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState>.Info;

    // A generic method of a sealed type cannot reach the operation's static members, so one instance per operation and
    // run types dispatches to them and names the operation in messages.
    private abstract class Members
    {
        public abstract void RequireCandidateWork(IOperator source, string metric);

        public abstract void RequireExecutor(IExecutor executor, object origin);

        public abstract IExecutor Wrap<TWrapper>(IExecutor executor, TWrapper wrapper) where TWrapper : struct, IInvocationWrapper;
    }

    private sealed class Members<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState> : Members
        where TOperation : IOperation<TOperation>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public override void RequireCandidateWork(IOperator source, string metric)
        {
            if (!TOperation.SupportsCandidateWork)
                throw new InvalidOperationException(
                    $"{metric} cannot measure candidate work for {source.GetType().Name}: {ExecutionSignature.Name(typeof(TOperation))} reports none.");
        }

        public override void RequireExecutor(IExecutor executor, object origin)
        {
            if (!TOperation.IsExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(executor))
                throw new InvalidOperationException($"{origin.GetType().Name} cannot provide a {ExecutionSignature.Name(typeof(TOperation))} " +
                    $"executor for these run types; it returned {executor.GetType()}.");
        }

        public override IExecutor Wrap<TWrapper>(IExecutor executor, TWrapper wrapper) =>
            TOperation.Wrap<TCandidate, TSearchSpace, TProblem, TSearchState, TWrapper>(executor, wrapper);
    }

    // Descriptions are immutable, so one instance serves every resolution with the same operation and run types.
    private static class Cache<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState>
        where TOperation : IOperation<TOperation>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        internal static readonly OperationInfo Info = new(
            TOperation.SupportsCandidateWork,
            new Members<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState>());
    }
}
