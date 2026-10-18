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
        return new Factory(ChildOperator, MaximumAttempts);
    }

    private sealed class Factory(IOperator<TOperation, TCandidate> child, int maximumAttempts)
        : OperatorExecutorFactory<TOperation, TCandidate>, IInvocationWrapper
    {
        public override ExecutorFactory GetExecutorFactory<TSearchSpace, TProblem, TRunSearchState>(
            OperationExecutorAdapter<TOperation, TCandidate, TSearchSpace, TProblem, TRunSearchState> adapter) =>
            adapter.Wrap(child, adapter.WrapInvocation(this));

        public Func<TArguments, TResult> Wrap<TArguments, TResult>(Func<TArguments, TResult> invokeChild,
            Func<TArguments, TResult, int>? countCandidateWork)
        {
            var attempts = maximumAttempts;
            return arguments => Retry.Execute(arguments, invokeChild, attempts);
        }
    }
}

public static class Retry
{
    public static Retry<TOperation, TCandidate> Create<TOperation, TCandidate>(IOperator<TOperation, TCandidate> childOperator, int maximumAttempts)
        where TOperation : IOperationDescriptor<TOperation> => new(childOperator, maximumAttempts);

    /// <summary>Invokes a typed delegate with the same arguments until it succeeds or exhausts its allowed attempts.</summary>
    /// <remarks>
    /// MaximumAttempts includes the first call. Only RetryableOperationException is retried; the last failure is
    /// rethrown unchanged. Arguments may be a value tuple containing the operation's inputs.
    /// Reuse a delegate created during executor construction to avoid a new closure for each call.
    /// This helper executes calls; it does not resolve or prepare child configurations.
    /// </remarks>
    public static TResult Execute<TArguments, TResult>(TArguments arguments, Func<TArguments, TResult> execute, int maximumAttempts)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumAttempts);
        for (var attempt = 1; ; attempt++)
        {
            try
            { return execute(arguments); }
            catch (RetryableOperationException) when (attempt < maximumAttempts) { }
        }
    }
}

public static class RetryOperatorExtensions
{
    public static Retry<TOperation, TCandidate> Retry<TOperation, TCandidate>(this IOperator<TOperation, TCandidate> source, int maximumAttempts)
        where TOperation : IOperationDescriptor<TOperation> => new(source, maximumAttempts);
}
