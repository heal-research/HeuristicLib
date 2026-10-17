using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Signals that an operation attempt failed and may be repeated by a configured retry wrapper.</summary>
public sealed class RetryableOperationException : Exception
{
    public RetryableOperationException(string message) : base(message) { }
    public RetryableOperationException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>Repeats a child operation when it throws RetryableOperationException, up to MaximumAttempts times.</summary>
/// <remarks>MaximumAttempts includes the first call. Attempts reuse the same arguments, child and RNG without rollback.
/// Successful calls return immediately. Other failures, including cancellation and aggregate exceptions, propagate without retry.</remarks>
public sealed record Retry<TOperation, TCandidate> : WrappingOperator<TOperation, TCandidate>
    where TOperation : IOperationDescriptor<TOperation>
{
    public Retry(IOperator<TOperation, TCandidate> childOperator, int maximumAttempts) : base(childOperator)
    {
        MaximumAttempts = maximumAttempts;
    }

    public int MaximumAttempts { get; init; }

    protected override OperatorExecutorFactory CreateOperatorFactory()
    {
        if (MaximumAttempts < 1)
            throw new InvalidOperationException("MaximumAttempts must be positive.");
        return new Factory(ChildOperator, new RetryPolicy(MaximumAttempts));
    }

    private sealed class Factory(IOperator<TOperation, TCandidate> child, SynchronousPolicy policy) : OperatorExecutorFactory<TOperation, TCandidate>
    {
        public override ExecutorFactory GetExecutorFactory<TSearchSpace, TProblem, TRunSearchState>(
            OperationExecutorAdapter<TOperation, TCandidate, TSearchSpace, TProblem, TRunSearchState> adapter) =>
            adapter.Wrap(child, adapter.PolicyWrapper(policy));
    }

    private sealed class RetryPolicy(int maximumAttempts) : SynchronousPolicy
    {
        public override OperationCallResult<TResult> Execute<TExecutor, TArguments, TResult>(
            ImmutableArray<TExecutor> children, TArguments arguments, IRandomNumberGenerator? random,
            Func<TExecutor, TArguments, OperationCallResult<TResult>> next)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                { return next(children[0], arguments); }
                catch (RetryableOperationException) when (attempt < maximumAttempts) { }
            }
        }
    }
}

public static class Retry
{
    public static Retry<TOperation, TCandidate> Create<TOperation, TCandidate>(IOperator<TOperation, TCandidate> childOperator, int maximumAttempts)
        where TOperation : IOperationDescriptor<TOperation> => new(childOperator, maximumAttempts);
}

public static class RetryOperatorExtensions
{
    public static Retry<TOperation, TCandidate> Retry<TOperation, TCandidate>(this IOperator<TOperation, TCandidate> source, int maximumAttempts)
        where TOperation : IOperationDescriptor<TOperation> => new(source, maximumAttempts);
}
