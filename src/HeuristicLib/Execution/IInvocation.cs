using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Execution;

/// <summary>One call to an executor's operation method together with its arguments, such as one call to Mutate.</summary>
/// <remarks>Each operation implements this on a private struct that holds the call's executor and arguments.</remarks>
public interface IInvocation<TResult>
{
    /// <summary>The random number generator passed to this call.</summary>
    IRandomNumberGenerator Random { get; }

    /// <summary>The search space passed to this call.</summary>
    ISearchSpace SearchSpace { get; }

    /// <summary>The problem passed to this call.</summary>
    IProblem Problem { get; }

    /// <summary>Calls the operation method with the original arguments, optionally on another executor or random number generator.</summary>
    /// <remarks>
    /// May be called more than once; every call reuses the same arguments. A null argument keeps the wrapped executor or
    /// the call's random number generator. Another executor must implement the same operation for the same run types,
    /// such as a sibling resolved by a choice.
    /// </remarks>
    TResult Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null);

    /// <summary>Returns the candidate work of a successful call, such as the number of mutated candidates.</summary>
    /// <remarks>
    /// Operations with candidate work implement this and report <see cref="IOperation{TSelf}.SupportsCandidateWork"/> as
    /// true; for other operations it throws <see cref="NotSupportedException"/>. The count describes this call's
    /// boundary; it does not imply that the call can be split into independent items.
    /// </remarks>
    int CountCandidateWork(TResult result) => throw new NotSupportedException();
}
