using HEAL.HeuristicLib.Execution;

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
    where TOperation : IOperation<TOperation>
{
    public Retry(IOperator<TOperation, TCandidate> childOperator, int maximumAttempts) : base(childOperator)
    {
        MaximumAttempts = maximumAttempts;
    }

    public int MaximumAttempts { get; init; }

    protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        if (MaximumAttempts < 1)
            throw new InvalidOperationException("MaximumAttempts must be positive.");
        return TOperation.Wrap<TCandidate, TSearchSpace, TProblem, TSearchState, RetryWrapper>(scope.ResolveOperator(ChildOperator), new RetryWrapper(MaximumAttempts));
    }
}

file readonly struct RetryWrapper(int maximumAttempts) : IInvocationWrapper
{
    public TResult Invoke<TInvocation, TResult>(TInvocation invocation) where TInvocation : struct, IInvocation<TResult>
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return invocation.Proceed();
            }
            catch (RetryableOperationException) when (attempt < maximumAttempts)
            {
                // Retry with the same arguments.
            }
        }
    }
}

public static class Retry
{
    public static Retry<TOperation, TCandidate> Create<TOperation, TCandidate>(IOperator<TOperation, TCandidate> childOperator, int maximumAttempts)
        where TOperation : IOperation<TOperation> => new(childOperator, maximumAttempts);
}

public static class RetryOperatorExtensions
{
    extension<TOperation, TCandidate>(IOperator<TOperation, TCandidate> source)
        where TOperation : IOperation<TOperation>
    {
        public Retry<TOperation, TCandidate> Retry(int maximumAttempts) => new(source, maximumAttempts);
    }
}
