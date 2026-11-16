using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Execution;

/// <summary>Describes one operation for the candidate and search-state types of one resolution, such as mutation.</summary>
/// <remarks>
/// A wrapper written for every operation, such as a counter, gets this from <see cref="For"/> to check and wrap its
/// child's executor without knowing the operation. The operation's own <see cref="IOperation{TSelf}"/> members supply
/// every answer.
/// </remarks>
public sealed class OperationInfo
{
    private readonly Members members;

    private OperationInfo(Members members)
    {
        this.members = members;
    }

    /// <summary>Whether the operation's invocations report candidate work.</summary>
    /// <remarks>A wrapper measuring candidate work checks this before it resolves its child.</remarks>
    public bool SupportsCandidateWork => members.SupportsCandidateWork;

    /// <summary>Rejects an operator whose operation reports no candidate work.</summary>
    public void RequireCandidateWork(IOperator source, string metric) => members.RequireCandidateWork(source, metric);

    /// <summary>Returns an executor whose named method passes each call, as an invocation, to the wrapper.</summary>
    public IExecutor Wrap<TWrapper>(IExecutor executor, TWrapper wrapper) where TWrapper : struct, IInvocationWrapper =>
        members.Wrap(executor, wrapper);

    /// <summary>Describes the operation for the candidate and search-state types, for a wrapper that builds its executor itself.</summary>
    public static OperationInfo For<TOperation, TCandidate, TSearchState>()
        where TOperation : IOperation<TOperation>
        where TSearchState : class, ISearchState =>
        Members<TOperation, TCandidate, TSearchState>.Instance;

    // Rejects an executor that does not implement the operation's executor interface for these run types.
    internal static void RequireExecutor<TOperation, TCandidate, TSearchState>(IExecutor executor, object origin)
        where TOperation : IOperation<TOperation>
        where TSearchState : class, ISearchState
    {
        if (!TOperation.IsExecutor<TCandidate, TSearchState>(executor))
            throw new InvalidOperationException($"{origin.GetType().Name} cannot provide a {ExecutionSignature.Name(typeof(TOperation))} " +
                $"executor for these run types; it returned {executor.GetType()}.");
    }

    // A generic method of a sealed type cannot reach the operation's static members, so one cached instance per operation,
    // candidate and search-state types dispatches to them and names the operation in messages.
    private abstract class Members
    {
        public abstract bool SupportsCandidateWork { get; }

        public abstract void RequireCandidateWork(IOperator source, string metric);

        public abstract IExecutor Wrap<TWrapper>(IExecutor executor, TWrapper wrapper) where TWrapper : struct, IInvocationWrapper;
    }

    private sealed class Members<TOperation, TCandidate, TSearchState> : Members
        where TOperation : IOperation<TOperation>
        where TSearchState : class, ISearchState
    {
        internal static OperationInfo Instance { get; } = new(new Members<TOperation, TCandidate, TSearchState>());

        public override bool SupportsCandidateWork => TOperation.SupportsCandidateWork;

        public override void RequireCandidateWork(IOperator source, string metric)
        {
            if (!TOperation.SupportsCandidateWork)
                throw new InvalidOperationException(
                    $"{metric} cannot measure candidate work for {source.GetType().Name}: {ExecutionSignature.Name(typeof(TOperation))} reports none.");
        }

        public override IExecutor Wrap<TWrapper>(IExecutor executor, TWrapper wrapper) =>
            TOperation.Wrap<TCandidate, TSearchState, TWrapper>(executor, wrapper);
    }
}
