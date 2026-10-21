using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Execution;

/// <summary>Adds behavior, such as timing, counting or retry, around calls to an executor's operation method.</summary>
/// <remarks>
/// An operation adapter builds the wrapper into the executor it constructs; each call to that executor's named method,
/// such as Mutate, passes one invocation to <see cref="Invoke{TInvocation, TResult}"/>. Implement wrappers as small
/// immutable structs so that each call is compiled for the wrapper type. Keep data that outlives a call, such as a
/// counter, in a referenced object. The wrapping executor forwards the wrapped executor's controls unless the wrapper
/// implements <see cref="IExecutorControlProvider"/>, which then supplies them.
/// </remarks>
public interface IInvocationWrapper
{
    /// <summary>Performs one invocation, calling <see cref="IInvocation{TResult}.Proceed"/> to run the wrapped method.</summary>
    TResult Invoke<TInvocation, TResult>(TInvocation invocation) where TInvocation : struct, IInvocation<TResult>;
}

/// <summary>One call to an executor's operation method together with its arguments, such as one call to Mutate.</summary>
public interface IInvocation<TResult>
{
    /// <summary>The random number generator passed to this call.</summary>
    IRandomNumberGenerator Random { get; }

    /// <summary>The search space passed to this call.</summary>
    ISearchSpace SearchSpace { get; }

    /// <summary>The problem passed to this call.</summary>
    IProblem Problem { get; }

    /// <summary>Calls the wrapped operation method with the original arguments.</summary>
    /// <remarks>May be called more than once; every call reuses the same arguments and random number generator.</remarks>
    TResult Proceed();

    /// <summary>Calls the operation method with the original arguments on another executor or random number generator.</summary>
    /// <remarks>
    /// A null argument keeps the wrapped executor or the call's random number generator. Another executor must implement
    /// the same operation for the same run types, such as a sibling resolved by a choice.
    /// </remarks>
    TResult ProceedWith(IExecutor? executor = null, IRandomNumberGenerator? random = null);

    /// <summary>Returns the candidate work of a successful call, such as the number of mutated candidates.</summary>
    /// <remarks>
    /// Operations with candidate work implement this and report <see cref="OperationExecutorAdapter.SupportsCandidateWork"/>
    /// as true; for other operations it throws <see cref="NotSupportedException"/>. The count describes this call's
    /// boundary; it does not imply that the call can be split into independent items.
    /// </remarks>
    int CountCandidateWork(TResult result) => throw new NotSupportedException();

    /// <summary>Runs each item of this call on the executor the router assigns it and returns the results in item order.</summary>
    /// <remarks>
    /// <see cref="IItemInvocation{TItemResult}"/> implements this for operations marked with
    /// <see cref="Operators.IItemOperation{TSelf}"/>; for other operations it throws <see cref="NotSupportedException"/>.
    /// </remarks>
    TResult ProceedPerItem(WeightedRouter<IExecutor> router) => throw new NotSupportedException();
}

/// <summary>A call whose items, such as parents or requested candidates, can run on different executors.</summary>
/// <remarks>
/// An operation marked with <see cref="Operators.IItemOperation{TSelf}"/> implements this on its invocation. The router
/// assigns the items, groups them per executor, restores their order and checks that each executor returns one result
/// per item.
/// </remarks>
public interface IItemInvocation<TItemResult> : IInvocation<IReadOnlyList<TItemResult>>
{
    /// <summary>The number of items in this call.</summary>
    int ItemCount { get; }

    /// <summary>Calls the operation method on another executor with only the given items, in the given order.</summary>
    /// <remarks>
    /// <paramref name="items"/> holds item positions in ascending order. The call keeps its random number generator, search
    /// space and problem, and must return one result per item.
    /// </remarks>
    IReadOnlyList<TItemResult> ProceedWithItems(IExecutor executor, IReadOnlyList<int> items);

    IReadOnlyList<TItemResult> IInvocation<IReadOnlyList<TItemResult>>.ProceedPerItem(WeightedRouter<IExecutor> router) =>
        router.Route(ItemCount, Random, (executor, items) => items.Count == ItemCount ? ProceedWith(executor) : ProceedWithItems(executor, items));
}
