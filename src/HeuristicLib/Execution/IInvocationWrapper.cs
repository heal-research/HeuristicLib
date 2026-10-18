namespace HEAL.HeuristicLib.Execution;

/// <summary>Adds behavior, such as timing, counting or retry, around calls to an executor's operation method.</summary>
/// <remarks>
/// An operation adapter builds the wrapper into the executor it constructs; each call to that executor's named method,
/// such as Mutate, passes one invocation to <see cref="Invoke{TInvocation, TResult}"/>. Implement wrappers as small
/// immutable structs so that each call is compiled for the wrapper type. Keep data that outlives a call, such as a
/// counter, in a referenced object.
/// </remarks>
public interface IInvocationWrapper
{
    /// <summary>Performs one invocation, calling <see cref="IInvocation{TResult}.Proceed"/> to run the wrapped method.</summary>
    TResult Invoke<TInvocation, TResult>(TInvocation invocation) where TInvocation : struct, IInvocation<TResult>;
}

/// <summary>One call to an executor's operation method together with its arguments, such as one call to Mutate.</summary>
public interface IInvocation<TResult>
{
    /// <summary>Calls the wrapped operation method with the original arguments.</summary>
    /// <remarks>May be called more than once; every call reuses the same arguments and random number generator.</remarks>
    TResult Proceed();

    /// <summary>Returns the candidate work of a successful call, such as the number of mutated candidates.</summary>
    /// <remarks>
    /// Operations without candidate work throw <see cref="NotSupportedException"/>, and their adapters report
    /// <see cref="OperationExecutorAdapter.SupportsCandidateWork"/> as false. The count describes this call's boundary;
    /// it does not imply that the call can be split into independent items.
    /// </remarks>
    int CountCandidateWork(TResult result);
}
